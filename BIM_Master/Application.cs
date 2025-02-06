using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace BIM_Master
{
    public class ApplicationClass : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("BIM Мастер");

            var panel = application.CreateRibbonPanel("BIM Мастер", "Очистка семейств");

            var button = new PushButtonData(
                "Удаление материалов",
                "Удалить материалы",
                "C:\\Users\\kiriliuksa\\source\\repos\\BIM_Master\\BIM_Master\\bin\\Release\\BIM_Master.dll",
                "DeleteMaterial.DeleteUnusedMaterials");
            BitmapImage image = new BitmapImage(new Uri("C:\\Users\\kiriliuksa\\source\\repos\\BIM_Master\\BIM_Master\\Icons\\IconDeleteMaterial16.png"));
            button.LargeImage = image;


            var button2 = new PushButtonData(
                "Подсказки",
                "Подсказки",
                "C:\\Users\\kiriliuksa\\source\\repos\\BIM_Master\\BIM_Master\\bin\\Release\\BIM_Master.dll",
                "FamilyParameterEditor.Command");
            BitmapImage image2 = new BitmapImage(new Uri("C:\\Users\\kiriliuksa\\source\\repos\\BIM_Master\\BIM_Master\\Icons\\Tip.png"));
            button2.LargeImage = image2;
            panel.AddItem(button);
            panel.AddItem(button2);




            return Result.Succeeded;
        }
    }
}
