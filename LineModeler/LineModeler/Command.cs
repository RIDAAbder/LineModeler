#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace LineModeler
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            var startPoint = uidoc.Selection.PickPoint(ObjectSnapTypes.Centers, "Pick the start point");
            var endPoint = uidoc.Selection.PickPoint(ObjectSnapTypes.Centers, "Pick the end point");

            var line = Line.CreateBound(startPoint, endPoint);
            Create3dModelLines(doc, line);

            return Result.Succeeded;
        }
        private static SketchPlane SketchPlanePassLine(Document doc, Line line)
        {
            XYZ StartPoint = line.GetEndPoint(0);
            XYZ EndPoint = line.GetEndPoint(1);

            Plane plane = Plane.CreateByThreePoints(StartPoint, EndPoint, XYZ.BasisX);
            SketchPlane skPlane = SketchPlane.Create(doc, plane); ;
            return skPlane;
        }
        private static bool Create3dModelLines(Document doc, Line line)
        {
            bool result;
            XYZ StartPoint = line.GetEndPoint(0);
            XYZ EndPoint = line.GetEndPoint(1);

            using (var transaction = new Transaction(doc, "Draw 3d Model Lines"))
            {


                transaction.Start("Draw 3d Model Lines");

                FailureHandlingOptions failureHandlingOptions = transaction.GetFailureHandlingOptions();
                failureHandlingOptions.SetFailuresPreprocessor(new MyFailureHandler());
                transaction.SetFailureHandlingOptions(failureHandlingOptions);

                if (null == line)
                {
                    throw new Exception("Geometry line creation failed.");
                }
                if (StartPoint.IsAlmostEqualTo(EndPoint))
                {
                    throw new Exception("Geometry line creation failed.");
                }
                doc.ActiveView.SketchPlane = SketchPlanePassLine(doc, line);
                doc.Create.NewModelCurve(line, SketchPlanePassLine(doc, line));
                transaction.Commit();

                result = true;
            }
            return result;
        }
    }
    public class MyFailureHandler : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            Document doc = failuresAccessor.GetDocument();
            foreach (var failureMessageAccessor in failuresAccessor.GetFailureMessages())
            {
                FailureDefinitionId failureDefinitionId = failureMessageAccessor.GetFailureDefinitionId();
                if (failureDefinitionId == BuiltInFailures.InaccurateFailures.InaccurateLine)
                {
                    failuresAccessor.DeleteWarning(failureMessageAccessor);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }

}
