//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using FamilyParameterEditor;
//using System.Collections.Generic;
//using System.Windows.Forms;
//using System;

//public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//{
//    try
//    {
//        UIApplication uiApp = commandData.Application;
//        UIDocument uiDoc = uiApp.ActiveUIDocument;
//        Document doc = uiDoc.Document;

//        if (!doc.IsFamilyDocument)
//        {
//            TaskDialog.Show("Ошибка", "Этот инструмент работает только в семействах!");
//            return Result.Cancelled;
//        }

//        FamilyManager familyManager = doc.FamilyManager;
//        List<FamilyParameter> parameters = new List<FamilyParameter>();

//        // Фильтруем параметры, исключая общие
//        foreach (FamilyParameter param in familyManager.Parameters)
//        {
//            if (!param.IsShared)
//            {
//                parameters.Add(param);
//            }
//        }

//        using (ParametersForm form = new ParametersForm(parameters))
//        {
//            if (form.ShowDialog() == DialogResult.OK)
//            {
//                using (Transaction t = new Transaction(doc, "Обновление описаний параметров"))
//                {
//                    t.Start();

//                    foreach (var item in form.ParametersData)
//                    {
//                        if (!string.IsNullOrEmpty(item.Description))
//                        {
//                            familyManager.SetDescription(item.Parameter, item.Description);
//                        }
//                    }

//                    t.Commit();
//                }
//            }
//        }

//        return Result.Succeeded;
//    }
//    catch (Exception ex)
//    {
//        message = ex.Message;
//        return Result.Failed;
//    }
////}