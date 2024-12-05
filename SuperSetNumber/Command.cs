#region License
/*Данный код опубликован под лицензией Creative Commons Attribution-ShareAlike.
Разрешено использовать, распространять, изменять и брать данный код за основу для производных в коммерческих и
некоммерческих целях, при условии указания авторства и если производные лицензируются на тех же условиях.
Код поставляется "как есть". Автор не несет ответственности за возможные последствия использования.
Зуев Александр, 2021, все права защищены.
This code is listed under the Creative Commons Attribution-ShareAlike license.
You may use, redistribute, remix, tweak, and build upon this work non-commercially and commercially,
as long as you credit the author by linking back and license your new creations under the same terms.
This code is provided 'as is'. Author disclaims any implied warranty.
Zuev Aleksandr, 2021, all rigths reserved.*/
#endregion
#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Structure;
using System.Xml.Serialization;
using System.IO;
using System.Data;
#endregion

namespace SuperSetNumber
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            char uchar = (char)8234;

            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<ElementId> selids = sel.GetElementIds().ToList();
            if(selids.Count == 0)
            {
                if(doc.ActiveView is ViewSheet)
                {
                    selids.Add(doc.ActiveView.Id);
                }
                else
                {
                    message = MyStrings.ErrorNoSelectedElements;
                    return Result.Failed;
                }
            }
            if (selids.Count > 1)
            {
                message = MyStrings.ErrorNoSelectedElements;
                return Result.Failed;
            }

            Element selem = doc.GetElement(selids[0]);
            BuiltInParameter numberParam = BuiltInParameter.INVALID;
            string labelText = "";

            if(selem is Autodesk.Revit.DB.Architecture.Room)
            {
                numberParam = BuiltInParameter.ROOM_NUMBER;
                labelText = MyStrings.LabelRoomNumber;
            }
            else if(selem is Grid)
            {
                numberParam = BuiltInParameter.DATUM_TEXT;
                labelText = MyStrings.LabelGridNumber;
            }
            else if(selem is Viewport)
            {
                numberParam = BuiltInParameter.VIEWPORT_DETAIL_NUMBER;
                labelText = MyStrings.LabelViewportNumber;
            }
            else if(selem is ViewSheet)
            {
                numberParam = BuiltInParameter.SHEET_NUMBER;
                labelText = MyStrings.LabelSheetNumber;
            }

            if(numberParam == BuiltInParameter.INVALID)
            {
                message = MyStrings.ErrorSupportedElements;
                return Result.Failed;
            }

            FormSetNumber form = new FormSetNumber(labelText);
            if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return Result.Cancelled;
            }
            string number = form.numbervalue;

            Type selemType = selem.GetType();
            if(selem is Autodesk.Revit.DB.Architecture.Room)
            {
                selemType = typeof(Autodesk.Revit.DB.SpatialElement);
            }
            List<Element> col = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfClass(selemType)
                .ToElements()
                .ToList();
            HashSet<string> values = new HashSet<string>();
            foreach(Element e in col)
            {
                Parameter p = e.get_Parameter(numberParam);
                if (p is null) continue;
                if (!p.HasValue) continue;
                if (p.StorageType != StorageType.String) continue;
                string val = p.AsString();
                values.Add(val);
            }

            int counter = 0;
            while(values.Contains(number))
            {
                number += uchar;
                counter++;
            }


            using (Transaction t = new Transaction(doc))
            {
                t.Start(MyStrings.TransactionName);

                Parameter finalNumberParam = selem.get_Parameter(numberParam);
                if(finalNumberParam is null)
                {
                    message = MyStrings.ErrorFailedToGetNumberParam;
                    return Result.Failed;
                }
                if(finalNumberParam.IsReadOnly)
                {
                    message = MyStrings.ErrorParamReadonly;
                    return Result.Failed;
                }

                finalNumberParam.Set(number);
                
                t.Commit();
            }


            return Result.Succeeded;
        }
    }
}
