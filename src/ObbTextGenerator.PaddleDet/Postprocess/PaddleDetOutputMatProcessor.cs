using OpenCvSharp;
using SkiaSharp;
using System.Diagnostics;

namespace ObbTextGenerator;

/// <summary>
/// Postprocesses the detector output <see cref="Mat"/> into runtime polygons.
/// This is the explicit place where CV2/OpenCV cleanup and contour-to-OBB logic should live.
/// </summary>
public static class PaddleDetOutputMatProcessor
{
    private const bool EnableDebugVisualization = false;

    private const float BitmapThreshold = 0.3f;
    private const float BoxScoreThreshold = 0.7f;
    private const float UnclipRatio = 2.0f;
    private const float MinimumBoxSide = 3.0f;
    private const int MaxCandidateCount = 1000;
    private const float MergeAngleDeltaDegrees = 12.0f;
    private const float MergeNormalOffsetInHeights = 0.75f;
    private const float MergeHeightRatio = 1.8f;
    private const float MergeGapInHeights = 2.5f;
    private const float MinimumMergedCoverageRatio = 0.45f;

    public static IReadOnlyList<SKPoint[]> ExtractObbPolygons(Mat maskMat)
    {
        ArgumentNullException.ThrowIfNull(maskMat);
        ValidateInputMaskFormat(maskMat);

        // СУТЬ: postprocess замеряем только на вычислительной части до визуального debug.
        // ЦЕЛЬ: получить чистое время contour/geometry/merge логики без влияния ImShow и WaitKey.
        var postprocessStopwatch = Stopwatch.StartNew();
        using var scoreMapPreview = CreateScoreMapPreview(maskMat);
        using var binaryMask = CreateBinaryMask(maskMat);
 
        Cv2.FindContours(
            binaryMask,
            out var contours,
            out _,
            RetrievalModes.List,
            ContourApproximationModes.ApproxSimple);

        var polygons = new List<SKPoint[]>();
        var candidateCount = Math.Min(contours.Length, MaxCandidateCount);

        for(var contourIndex = 0; contourIndex < candidateCount; contourIndex++)
        {
            var contour = contours[contourIndex];
            if(contour.Length < 3)
            {
                continue;
            }

            // СУТЬ: как и в PaddleOCR, сначала берём минимальный прямоугольник вокруг компоненты,
            // чтобы быстро получить устойчивый OBB-кандидат и его характерный размер.
            // ЦЕЛЬ: сразу отсечь слишком маленький шум до более дорогих шагов.
            var miniBox = GetMiniBox(contour);
            if(miniBox.ShortSide < MinimumBoxSide)
            {
                continue;
            }

            // СУТЬ: у DB-постпроцессинга оценка кандидата считается не по всему кадру,
            // а по среднему score внутри локального полигона.
            // ЦЕЛЬ: оставить только те области, где сама probability map уверенно поддерживает текст.
            var score = ComputeBoxScoreFast(maskMat, miniBox.Points);
            if(score < BoxScoreThreshold)
            {
                continue;
            }

            // СУТЬ: оригинальный DBPostProcess делает unclip, то есть расширяет найденный контур наружу,
            // потому что бинаризация обычно даёт более узкое ядро текста, чем нужен итоговый бокс.
            // ЦЕЛЬ: вернуть блок примерно к полному охвату текстовой области перед финальным OBB.
            var expandedPolygon = ExpandMiniBox(miniBox.Points, UnclipRatio);
            if(expandedPolygon.Length < 4)
            {
                continue;
            }

            // СУТЬ: после расширения PaddleOCR ещё раз приводит фигуру к min area rect.
            // ЦЕЛЬ: получить стабильный итоговый OBB даже если расширение слегка исказило форму.
            var expandedMiniBox = GetMiniBox(expandedPolygon);
            if(expandedMiniBox.ShortSide < MinimumBoxSide + 2.0f)
            {
                continue;
            }

            polygons.Add(ConvertToSkPoints(expandedMiniBox.Points));
        }

        // СУТЬ: DB-постпроцессинг хорошо выделяет локальные текстовые островки,
        // но для обучения line-like OBB нам нужно ещё попытаться собрать близкие куски одной строки.
        // ЦЕЛЬ: объединять только те компоненты, которые реально похожи на один компактный строковый блок,
        // и при этом не склеивать слишком разреженные последовательности в токсичный для YOLO большой пустой OBB.
        var mergedPolygons = MergeLinePolygons(polygons);
        postprocessStopwatch.Stop();
        Debug.WriteLine(
            $"[PaddleDet] Postprocess: {postprocessStopwatch.Elapsed.TotalMilliseconds:F3} ms (candidates={polygons.Count}, merged={mergedPolygons.Count}, mask={maskMat.Width}x{maskMat.Height})");

        if(EnableDebugVisualization)
        {
            using var binaryMaskPreview = binaryMask.Clone();
            using var candidateOverlayPreview = CreatePolygonOverlayPreview(scoreMapPreview, polygons);
            using var mergedOverlayPreview = CreatePolygonOverlayPreview(scoreMapPreview, mergedPolygons);

            // СУТЬ: весь визуальный debug собран в одном переключаемом блоке.
            // ЦЕЛЬ: быстро включать и выключать диагностический вывод без поиска отдельных ImShow по методу.
            Cv2.ImShow("paddledet.score_map", scoreMapPreview);
            Cv2.ImShow("paddledet.binary_mask", binaryMaskPreview);
            Cv2.ImShow("paddledet.obb_candidates", candidateOverlayPreview);
            Cv2.ImShow("paddledet.obb_merged", mergedOverlayPreview);
            Cv2.WaitKey(0);
        }

        return mergedPolygons;
    }

    private static void ValidateInputMaskFormat(Mat maskMat)
    {
        // СУТЬ: здесь мы жёстко фиксируем runtime-контракт для постпроцессинга.
        // ЦЕЛЬ: сразу падать на неверном входе, а не молча конвертировать данные и маскировать ошибку выше по пайплайну.
        if(maskMat.Type() != MatType.CV_32FC1)
        {
            throw new InvalidOperationException(
                $"PaddleDet postprocess expects input Mat of type {MatType.CV_32FC1}, but got {maskMat.Type()}.");
        }
    }

    private static Mat CreateBinaryMask(Mat scoreMap)
    {
        var binaryMask = new Mat();

        // СУТЬ: это прямой аналог segmentation = pred > thresh в PaddleOCR.
        // ЦЕЛЬ: выделить компоненты, которые потом пойдут в contour-based извлечение кандидатов.
        Cv2.Threshold(scoreMap, binaryMask, BitmapThreshold, 255.0, ThresholdTypes.Binary);
        binaryMask.ConvertTo(binaryMask, MatType.CV_8UC1);

        return binaryMask;
    }

    private static Mat CreateScoreMapPreview(Mat scoreMap)
    {
        var previewMat = new Mat();

        // СУТЬ: score map удобнее смотреть как обычную grayscale-картинку в диапазоне 0..255.
        // ЦЕЛЬ: визуально видеть, какие зоны детектор считает сильным текстовым сигналом ещё до бинаризации.
        scoreMap.ConvertTo(previewMat, MatType.CV_8UC1, 255.0);
        return previewMat;
    }

    private static Mat CreatePolygonOverlayPreview(Mat scoreMapPreview, IReadOnlyList<SKPoint[]> polygons)
    {
        var overlayMat = new Mat();

        // СУТЬ: поверх score map рисуем уже финальные OBB после всех фильтров.
        // ЦЕЛЬ: быстро сравнивать исходный отклик модели и итоговые блоки, которые реально вышли из postprocess.
        Cv2.CvtColor(scoreMapPreview, overlayMat, ColorConversionCodes.GRAY2BGR);

        foreach(var polygon in polygons)
        {
            var contour = polygon
                .Select(point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y)))
                .ToArray();

            Cv2.Polylines(overlayMat, new[] { contour }, true, new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);
        }

        return overlayMat;
    }

    private static IReadOnlyList<SKPoint[]> MergeLinePolygons(IReadOnlyList<SKPoint[]> sourcePolygons)
    {
        if(sourcePolygons.Count <= 1)
        {
            return sourcePolygons.ToList();
        }

        var clusters = sourcePolygons
            .Select(CreatePolygonComponent)
            .Select(component => new List<PolygonComponent> { component })
            .ToList();

        bool mergedAnyCluster = false;

        // СУТЬ: объединяем кластеры жадно и итеративно, пока ещё находятся пары,
        // которые проходят все line-merge ограничения.
        // ЦЕЛЬ: получить естественные строковые группы без сложного графового солвера и без лишней магии.
        do
        {
            mergedAnyCluster = false;

            for(var leftClusterIndex = 0; leftClusterIndex < clusters.Count && !mergedAnyCluster; leftClusterIndex++)
            {
                for(var rightClusterIndex = leftClusterIndex + 1; rightClusterIndex < clusters.Count; rightClusterIndex++)
                {
                    var leftCluster = clusters[leftClusterIndex];
                    var rightCluster = clusters[rightClusterIndex];

                    if(!CanMergeClusters(leftCluster, rightCluster))
                    {
                        continue;
                    }

                    // СУТЬ: после успешной проверки просто переносим компоненты в один кластер
                    // и запускаем новый проход, потому что геометрия объединённого блока уже изменилась.
                    // ЦЕЛЬ: дать цепочке близких фрагментов строки собраться постепенно, а не только попарно.
                    leftCluster.AddRange(rightCluster);
                    clusters.RemoveAt(rightClusterIndex);
                    mergedAnyCluster = true;
                    break;
                }
            }
        }
        while(mergedAnyCluster);

        var mergedPolygons = new List<SKPoint[]>(clusters.Count);

        foreach(var cluster in clusters)
        {
            var clusterInfo = BuildClusterInfo(cluster);
            mergedPolygons.Add(ConvertToSkPoints(clusterInfo.Polygon));
        }

        return mergedPolygons;
    }

    private static bool CanMergeClusters(
        IReadOnlyList<PolygonComponent> leftCluster,
        IReadOnlyList<PolygonComponent> rightCluster)
    {
        var leftInfo = BuildClusterInfo(leftCluster);
        var rightInfo = BuildClusterInfo(rightCluster);
        var sharedAxis = GetSharedClusterAxis(leftInfo, rightInfo);
        var sharedNormal = new Point2f(-sharedAxis.Y, sharedAxis.X);
        var centerDelta = new Point2f(
            rightInfo.Center.X - leftInfo.Center.X,
            rightInfo.Center.Y - leftInfo.Center.Y);

        // СУТЬ: все относительные метрики нормализуем на характерную высоту строки.
        // ЦЕЛЬ: одно и то же правило одинаково работало и на мелком, и на крупном тексте.
        var meanHeight = (leftInfo.MeanHeight + rightInfo.MeanHeight) * 0.5f;
        if(meanHeight <= float.Epsilon)
        {
            return false;
        }

        var angleDeltaDegrees = GetAngleDeltaDegrees(leftInfo.Axis, rightInfo.Axis);
        if(angleDeltaDegrees > MergeAngleDeltaDegrees)
        {
            return false;
        }

        var normalOffset = MathF.Abs(Dot(centerDelta, sharedNormal));
        var normalizedNormalOffset = normalOffset / meanHeight;
        if(normalizedNormalOffset > MergeNormalOffsetInHeights)
        {
            return false;
        }

        var heightRatio = Math.Max(leftInfo.MeanHeight, rightInfo.MeanHeight) / Math.Min(leftInfo.MeanHeight, rightInfo.MeanHeight);
        if(heightRatio > MergeHeightRatio)
        {
            return false;
        }

        var gapAlongAxis = GetGapAlongAxis(leftInfo.Polygon, rightInfo.Polygon, sharedAxis);
        var normalizedGapAlongAxis = gapAlongAxis / meanHeight;
        if(normalizedGapAlongAxis > MergeGapInHeights)
        {
            return false;
        }

        var mergedClusterComponents = new List<PolygonComponent>(leftCluster.Count + rightCluster.Count);
        mergedClusterComponents.AddRange(leftCluster);
        mergedClusterComponents.AddRange(rightCluster);

        var mergedInfo = BuildClusterInfo(mergedClusterComponents);

        // СУТЬ: coverageRatio показывает, насколько объединённый OBB реально заполнен исходными компонентами.
        // ЦЕЛЬ: не собирать чрезмерно длинные и пустые боксы, которые плохо подходят как train target для YOLO OBB.
        if(mergedInfo.CoverageRatio < MinimumMergedCoverageRatio)
        {
            return false;
        }

        return true;
    }

    private static PolygonComponent CreatePolygonComponent(SKPoint[] polygon)
    {
        var cvPoints = polygon
            .Select(point => new Point2f(point.X, point.Y))
            .ToArray();

        var miniBox = GetMiniBox(cvPoints);
        var center = GetCentroid(miniBox.Points);
        var axis = GetDominantAxis(miniBox.Points);
        var area = Math.Abs(GetPolygonArea(miniBox.Points));

        return new PolygonComponent(miniBox.Points, center, axis, miniBox.ShortSide, area);
    }

    private static ClusterInfo BuildClusterInfo(IReadOnlyList<PolygonComponent> cluster)
    {
        var allPoints = cluster
            .SelectMany(component => component.Polygon)
            .ToArray();

        var mergedMiniBox = GetMiniBox(allPoints);
        var mergedCenter = GetCentroid(mergedMiniBox.Points);
        var mergedAxis = GetDominantAxis(mergedMiniBox.Points);
        var meanHeight = cluster.Average(component => component.Height);
        var summedComponentArea = cluster.Sum(component => component.Area);
        var mergedArea = Math.Abs(GetPolygonArea(mergedMiniBox.Points));
        var coverageRatio = mergedArea <= float.Epsilon ? 0.0f : summedComponentArea / mergedArea;

        // СУТЬ: агрегированная информация по кластеру считается через общий min area rect всех компонент.
        // ЦЕЛЬ: линия после merge должна опираться уже на общую геометрию кластера, а не на случайный одиночный бокс.
        return new ClusterInfo(
            mergedMiniBox.Points,
            mergedCenter,
            mergedAxis,
            meanHeight,
            summedComponentArea,
            mergedArea,
            coverageRatio);
    }

    private static Point2f GetSharedClusterAxis(ClusterInfo leftInfo, ClusterInfo rightInfo)
    {
        var axis = leftInfo.SummedComponentArea >= rightInfo.SummedComponentArea
            ? leftInfo.Axis
            : rightInfo.Axis;

        // СУТЬ: общую ось берём у более "тяжёлого" кластера по суммарной площади компонент.
        // ЦЕЛЬ: уменьшить влияние маленьких и шумных боксов на направление merge.
        return Normalize(axis);
    }

    private static float GetGapAlongAxis(Point2f[] leftPolygon, Point2f[] rightPolygon, Point2f axis)
    {
        var leftInterval = GetProjectionInterval(leftPolygon, axis);
        var rightInterval = GetProjectionInterval(rightPolygon, axis);

        // СУТЬ: вдоль строки нас интересует именно пустой промежуток между интервалами проекции,
        // а не дистанция между центрами, которая плохо работает на боксы разной длины.
        // ЦЕЛЬ: корректно объединять почти касающиеся слова и не склеивать слишком дальние фрагменты.
        if(leftInterval.Maximum < rightInterval.Minimum)
        {
            return rightInterval.Minimum - leftInterval.Maximum;
        }

        if(rightInterval.Maximum < leftInterval.Minimum)
        {
            return leftInterval.Minimum - rightInterval.Maximum;
        }

        return 0.0f;
    }

    private static ProjectionInterval GetProjectionInterval(Point2f[] polygon, Point2f axis)
    {
        var minimum = float.MaxValue;
        var maximum = float.MinValue;

        for(var index = 0; index < polygon.Length; index++)
        {
            var projection = Dot(polygon[index], axis);
            if(projection < minimum)
            {
                minimum = projection;
            }

            if(projection > maximum)
            {
                maximum = projection;
            }
        }

        return new ProjectionInterval(minimum, maximum);
    }

    private static Point2f GetDominantAxis(Point2f[] polygon)
    {
        var edge01 = new Point2f(polygon[1].X - polygon[0].X, polygon[1].Y - polygon[0].Y);
        var edge12 = new Point2f(polygon[2].X - polygon[1].X, polygon[2].Y - polygon[1].Y);
        var edge01Length = GetVectorLength(edge01);
        var edge12Length = GetVectorLength(edge12);
        var dominantEdge = edge01Length >= edge12Length ? edge01 : edge12;

        // СУТЬ: ось строки у OBB берём по его длинной стороне.
        // ЦЕЛЬ: получить локальное направление блока в той системе координат, в которой реально лежит текст.
        return Normalize(dominantEdge);
    }

    private static Point2f Normalize(Point2f vector)
    {
        var length = GetVectorLength(vector);
        if(length <= float.Epsilon)
        {
            return new Point2f(1.0f, 0.0f);
        }

        return new Point2f(vector.X / length, vector.Y / length);
    }

    private static float GetVectorLength(Point2f vector)
    {
        return MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    }

    private static float Dot(Point2f left, Point2f right)
    {
        return left.X * right.X + left.Y * right.Y;
    }

    private static float GetAngleDeltaDegrees(Point2f leftAxis, Point2f rightAxis)
    {
        var normalizedLeftAxis = Normalize(leftAxis);
        var normalizedRightAxis = Normalize(rightAxis);
        var dot = Math.Clamp(MathF.Abs(Dot(normalizedLeftAxis, normalizedRightAxis)), 0.0f, 1.0f);
        var angleRadians = MathF.Acos(dot);
        return angleRadians * 180.0f / MathF.PI;
    }

    private static MiniBoxInfo GetMiniBox(Point[] contour)
    {
        var rotatedRect = Cv2.MinAreaRect(contour);
        var rawPoints = rotatedRect.Points();

        // СУТЬ: PaddleOCR упорядочивает вершины прямоугольника в согласованном порядке.
        // ЦЕЛЬ: downstream-код получает одинаковую геометрию независимо от внутреннего порядка OpenCV.
        var orderedPoints = OrderClockwise(rawPoints);
        var shortSide = Math.Min(rotatedRect.Size.Width, rotatedRect.Size.Height);

        return new MiniBoxInfo(orderedPoints, shortSide);
    }

    private static MiniBoxInfo GetMiniBox(Point2f[] polygon)
    {
        var rotatedRect = Cv2.MinAreaRect(polygon);
        var rawPoints = rotatedRect.Points();

        // СУТЬ: после расширения снова нормализуем фигуру через min area rect.
        // ЦЕЛЬ: стабилизировать финальный OBB и убрать артефакты локальной аппроксимации расширения.
        var orderedPoints = OrderClockwise(rawPoints);
        var shortSide = Math.Min(rotatedRect.Size.Width, rotatedRect.Size.Height);

        return new MiniBoxInfo(orderedPoints, shortSide);
    }

    private static float ComputeBoxScoreFast(Mat scoreMap, Point2f[] box)
    {
        var bounds = GetClampedBounds(scoreMap, box);
        if(bounds.Width <= 0 || bounds.Height <= 0)
        {
            return 0.0f;
        }

        using var mask = new Mat(bounds.Height, bounds.Width, MatType.CV_8UC1, Scalar.Black);
        var localPoints = new Point[box.Length];

        for(var index = 0; index < box.Length; index++)
        {
            var point = box[index];
            var localX = (int)Math.Round(point.X - bounds.X);
            var localY = (int)Math.Round(point.Y - bounds.Y);
            localPoints[index] = new Point(localX, localY);
        }

        // СУТЬ: fast-score берёт среднее значение карты только внутри полигона кандидата.
        // ЦЕЛЬ: дёшево повторить логику PaddleOCR без прохода по всему контуру на уровне пикселей.
        Cv2.FillPoly(mask, new[] { localPoints }, Scalar.White);

        using var scoreRegion = new Mat(scoreMap, bounds);
        var meanScore = Cv2.Mean(scoreRegion, mask);
        return (float)meanScore.Val0;
    }

    private static Rect GetClampedBounds(Mat scoreMap, Point2f[] box)
    {
        var widthLimit = scoreMap.Width - 1;
        var heightLimit = scoreMap.Height - 1;

        var minX = (int)Math.Clamp(Math.Floor(box.Min(point => point.X)), 0.0, widthLimit);
        var maxX = (int)Math.Clamp(Math.Ceiling(box.Max(point => point.X)), 0.0, widthLimit);
        var minY = (int)Math.Clamp(Math.Floor(box.Min(point => point.Y)), 0.0, heightLimit);
        var maxY = (int)Math.Clamp(Math.Ceiling(box.Max(point => point.Y)), 0.0, heightLimit);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        return new Rect(minX, minY, width, height);
    }

    private static Point2f[] ExpandMiniBox(Point2f[] points, float unclipRatio)
    {
        var area = Math.Abs(GetPolygonArea(points));
        var perimeter = GetPolygonPerimeter(points);
        if(area <= 0.0f || perimeter <= 0.0f)
        {
            return [];
        }

        // СУТЬ: формула расстояния такая же по смыслу, как в DBPostProcess: area * ratio / perimeter.
        // ЦЕЛЬ: увеличивать маленькие и большие блоки пропорционально их собственной геометрии.
        var expansionDistance = area * unclipRatio / perimeter;
        var center = GetCentroid(points);
        var expandedPoints = new Point2f[points.Length];

        for(var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            var offsetX = point.X - center.X;
            var offsetY = point.Y - center.Y;
            var length = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY);

            if(length <= float.Epsilon)
            {
                expandedPoints[index] = point;
                continue;
            }

            var scale = (length + expansionDistance) / length;
            expandedPoints[index] = new Point2f(
                center.X + offsetX * scale,
                center.Y + offsetY * scale);
        }

        return expandedPoints;
    }

    private static float GetPolygonArea(Point2f[] points)
    {
        var doubledArea = 0.0f;

        // СУТЬ: площадь нужна для DB-формулы расширения контура.
        // ЦЕЛЬ: получить scale-aware величину, которая учитывает реальный размер кандидата.
        for(var index = 0; index < points.Length; index++)
        {
            var currentPoint = points[index];
            var nextPoint = points[(index + 1) % points.Length];
            doubledArea += currentPoint.X * nextPoint.Y - nextPoint.X * currentPoint.Y;
        }

        return doubledArea * 0.5f;
    }

    private static float GetPolygonPerimeter(Point2f[] points)
    {
        var perimeter = 0.0f;

        // СУТЬ: периметр вместе с площадью задаёт, насколько сильно надо раздвинуть контур наружу.
        // ЦЕЛЬ: сделать расширение инвариантным к форме, а не только к абсолютному размеру.
        for(var index = 0; index < points.Length; index++)
        {
            var currentPoint = points[index];
            var nextPoint = points[(index + 1) % points.Length];
            perimeter += MathF.Sqrt(
                (nextPoint.X - currentPoint.X) * (nextPoint.X - currentPoint.X)
                + (nextPoint.Y - currentPoint.Y) * (nextPoint.Y - currentPoint.Y));
        }

        return perimeter;
    }

    private static Point2f GetCentroid(Point2f[] points)
    {
        var sumX = 0.0f;
        var sumY = 0.0f;

        // СУТЬ: локально расширяем прямоугольник от его центра.
        // ЦЕЛЬ: получить простую и устойчивую аппроксимацию unclip без внешней polygon-offset библиотеки.
        for(var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            sumX += point.X;
            sumY += point.Y;
        }

        return new Point2f(sumX / points.Length, sumY / points.Length);
    }

    private static Point2f[] OrderClockwise(Point2f[] points)
    {
        var center = GetCentroid(points);
        var orderedPoints = points
            .OrderBy(point => MathF.Atan2(point.Y - center.Y, point.X - center.X))
            .ToArray();

        var topLeftIndex = 0;
        var topLeftMetric = float.MaxValue;

        for(var index = 0; index < orderedPoints.Length; index++)
        {
            var point = orderedPoints[index];
            var metric = point.X + point.Y;
            if(metric < topLeftMetric)
            {
                topLeftMetric = metric;
                topLeftIndex = index;
            }
        }

        // СУТЬ: после сортировки по углу переносим старт в top-left.
        // ЦЕЛЬ: получить предсказуемый обход вершин для записи аннотаций и последующих стадий.
        return Enumerable
            .Range(0, orderedPoints.Length)
            .Select(index => orderedPoints[(topLeftIndex + index) % orderedPoints.Length])
            .ToArray();
    }

    private static SKPoint[] ConvertToSkPoints(Point2f[] points)
    {
        var polygon = new SKPoint[points.Length];

        // СУТЬ: на выходе процессор переводит OpenCV-геометрию в runtime-формат генератора.
        // ЦЕЛЬ: дальше пайплайн работает только с SKPoint[] и не зависит от OpenCV-типов.
        for(var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            polygon[index] = new SKPoint(point.X, point.Y);
        }

        return polygon;
    }

    private readonly record struct MiniBoxInfo(Point2f[] Points, float ShortSide);
    private readonly record struct PolygonComponent(
        Point2f[] Polygon,
        Point2f Center,
        Point2f Axis,
        float Height,
        float Area);

    private readonly record struct ClusterInfo(
        Point2f[] Polygon,
        Point2f Center,
        Point2f Axis,
        float MeanHeight,
        float SummedComponentArea,
        float MergedArea,
        float CoverageRatio);

    private readonly record struct ProjectionInterval(float Minimum, float Maximum);
}

