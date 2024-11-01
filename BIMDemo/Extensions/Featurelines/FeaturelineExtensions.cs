using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBObject = Autodesk.AutoCAD.DatabaseServices.DBObject;

namespace BIMDemo.Extensions.Featurelines
{
    public static class FeaturelineExtensions
    {
        public static List<FeatureLine> SplitFeatureLine(this FeatureLine featureLine, Point3dCollection splitPoints)
        {
            var splitFeatureLines = new List<FeatureLine>();

            var flElevPtData = featureLine.GetFeatureLinePointData(splitPoints);

            using (var featurelineClone = featureLine.Clone() as FeatureLine)
            {
                featurelineClone.FlattenPointData(true);

                using (var projectedCurve = featurelineClone.GetFlatCurve())
                {
                    var splitParams = projectedCurve.GetParamsAtPoints(splitPoints);

                    if (splitParams.Count == 0)
                    {
                        return splitFeatureLines;
                    }

                    var splitCurves = projectedCurve.GetSplitCurves(splitParams);

                    if (splitCurves.Count == 0)
                    {
                        return splitFeatureLines;
                    }

                    var startParam = featureLine.StartParam;
                    
                    foreach (Curve splitCurve in splitCurves)
                    {
                        double endParam = double.NaN;
                        if (splitParams.Count == 0)
                        {
                            endParam = featureLine.EndParam;
                        }
                        else
                        {
                            endParam = splitParams[0];
                            splitParams.RemoveAt(0);
                        }
                        
                        var splitCurveObjId = splitCurve.AddToModelSpace();

                        var splitFeatureLineObjid = featureLine.SiteId.IsNull ?
                                                        FeatureLine.Create(null, splitCurveObjId) :
                                                        FeatureLine.Create(null, splitCurveObjId, featureLine.SiteId);

                        var deleteCurve = splitCurveObjId.GetObject(OpenMode.ForWrite);
                        deleteCurve.Erase();

                        var splitFeatureLine = splitFeatureLineObjid.GetObject(OpenMode.ForWrite) as FeatureLine;

                        var ptsToAdjust = flElevPtData.Where(ptData => ptData.Parameter >= startParam && 
                                                             ptData.Parameter <= endParam)
                                                       .ToList();

                        splitFeatureLine.AssignFeatureLinePointData(ptsToAdjust);

                        splitFeatureLine.LayerId = featureLine.LayerId;
                        splitFeatureLine.LinetypeId = featureLine.LinetypeId;
                        splitFeatureLine.LinetypeScale = featureLine.LinetypeScale;
                        splitFeatureLine.LineWeight = featureLine.LineWeight;
                        splitFeatureLine.MaterialId = featureLine.MaterialId;
                        if (featureLine.PlotStyleName != "ByLayer")
                        {
                            splitFeatureLine.PlotStyleNameId = featureLine.PlotStyleNameId;
                        }

                        splitFeatureLine.ReceiveShadows = featureLine.ReceiveShadows;
                        splitFeatureLine.ShowToolTip = featureLine.ShowToolTip;

                        splitFeatureLine.StyleId = featureLine.StyleId;

                        splitFeatureLine.Transparency = featureLine.Transparency;
                        splitFeatureLine.VisualStyleId = featureLine.VisualStyleId;
                        splitFeatureLine.XData = featureLine.XData;

                        splitFeatureLines.Add(splitFeatureLine);

                        startParam = endParam;
                    }
                }
            }

            return splitFeatureLines;
        }

        /// <summary>
        /// Get point info for a feature line. 
        /// </summary>
        /// <param name="fl">Feature line to be used</param>
        /// <param name="splitPts">Additional points to return, used for trim commands.</param>
        /// <returns></returns>
        public static List<FeatureLinePointData> GetFeatureLinePointData(this FeatureLine fl, Point3dCollection splitPts)
        {
            var flPtDatas = new List<FeatureLinePointData>();

            ProcessElevationPoints(fl, FeatureLinePointType.ElevationPoint, flPtDatas);
            ProcessElevationPoints(fl, FeatureLinePointType.PIPoint, flPtDatas);
            ProcessElevationPoints(fl, splitPts, flPtDatas);
            return flPtDatas;
        }

        private static void ProcessElevationPoints(FeatureLine fl, FeatureLinePointType flPtType, List<FeatureLinePointData> flPtDatas)
        {
            foreach (Point3d pt in fl.GetPoints(flPtType))
            {
                var flPtData = new FeatureLinePointData()
                {
                    Parameter = fl.GetParameterAtPoint(pt),
                    Location = pt,
                    PointType = flPtType
                };

                flPtDatas.Add(flPtData);
            }
        }

        private static void ProcessElevationPoints(FeatureLine fl, Point3dCollection splitPts, List<FeatureLinePointData> flPtDatas)
        {
            foreach (Point3d pt in splitPts)
            {
                var closestPt = fl.GetClosestPointToProjected(pt);
                var param = fl.GetParameterAtPoint(closestPt);

                var flPtData = new FeatureLinePointData()
                {
                    Parameter = param,
                    Location = closestPt,
                    PointType = FeatureLinePointType.PIPoint
                };

                flPtDatas.Add(flPtData);
            }
        }

        /// <summary>
        /// Need to flatten the feature line in order to either find the parameter given a 2d point or get a feature line to 
        /// create a polyline with curves.
        /// </summary>
        /// <param name="fl"></param>
        /// <param name="ptDatas"></param>
        /// <param name="shouldDeleteElvPts">False to keep parameter values and have a flat feature line, True to extract a polyline.</param>
        public static void FlattenPointData(this FeatureLine fl, bool shouldDeleteElvPts)
        {
            var elevPts = fl.GetPoints(FeatureLinePointType.ElevationPoint);
            if (shouldDeleteElvPts)
            {
                DeleteElevData(fl, elevPts);
            }
            else
            {
                FlattenPtPIData(fl, elevPts);
            }

            var piPts = fl.GetPoints(FeatureLinePointType.PIPoint);
            FlattenPtPIData(fl, piPts);
        }

        private static void DeleteElevData(FeatureLine fl, Point3dCollection ptColl)
        {
            foreach (Point3d pt in ptColl)
            {
                try
                {
                    fl.DeleteElevationPoint(pt);
                }
                catch (System.Exception ex)
                {

                    BIMDemoApp.WriteErrorMessage("SplitFeatureLines", ex);
                    BIMDemoApp.WriteMessage("\nNot an elevation point?: " + pt.X + "," + pt.Y + "," + pt.Z);
                }
            }
        }

        private static void FlattenPtPIData(FeatureLine fl, Point3dCollection ptColl)
        {
            foreach (Point3d pt in ptColl)
            {
                try
                {
                    var param = fl.GetParameterAtPoint(pt);
                    fl.SetPointElevation((int)param, 0);
                }
                catch (Exception ex)
                {
#if DEBUG
                    BIMDemoApp.WriteErrorMessage("SplitFeatureLines", ex);
#endif
                }
            }
        }

        public static Curve GetFlatCurve(this FeatureLine fl)
        {
            Plane planeXY = new Plane(new Point3d(0, 0, 0), Vector3d.ZAxis);

            var flatCurve = fl.GetProjectedCurve(planeXY, planeXY.Normal);

            if (flatCurve is Polyline2d)
            {
                return flatCurve.ToPolyline();
            }

            return flatCurve;

        }

        public static void AssignFeatureLinePointData(this FeatureLine flSeg, IEnumerable<FeatureLinePointData> ptsToAdjust)
        {
            foreach (var ptToAdjust in ptsToAdjust)
            {
                switch (ptToAdjust.PointType)
                {
                    case Autodesk.Civil.FeatureLinePointType.AllPoints:
                        break;
                    case Autodesk.Civil.FeatureLinePointType.ElevationPoint:
                        try
                        {
                            flSeg.InsertElevationPoint(ptToAdjust.Location);
                        }
                        catch (System.Exception ex)
                        {
                            BIMDemoApp.WriteErrorMessage("AssignFeatureLinePointData", ex);
                        }
                        break;
                    case Autodesk.Civil.FeatureLinePointType.PIPoint:
                        try
                        {
                            var paramToUse = flSeg.GetParameterAtPoint(ptToAdjust.Location);
                            flSeg.SetPointElevation((int)paramToUse, ptToAdjust.Location.Z);
                        }
                        catch (System.Exception ex)
                        {
                            BIMDemoApp.WriteErrorMessage("AssignFeatureLinePointData", ex);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
