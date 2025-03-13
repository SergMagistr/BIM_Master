using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using System.Reflection;

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
            // Создаем вкладку
            application.CreateRibbonTab("BIM Мастер");

            var panel = application.CreateRibbonPanel("BIM Мастер", "Очистка семейств");
            var panel2 = application.CreateRibbonPanel("BIM Мастер", "Тестовый стенд");

            // Получаем путь к директории плагина
            string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Создание кнопок
            var button = new PushButtonData(
                "Удаление материалов",
                "Удалить\nматериалы",
                pluginPath + "\\BIM_Master.dll",
                "DeleteMaterial.DeleteUnusedMaterials");

            // Загружаем иконку как встроенный ресурс
            BitmapImage image = LoadImageFromResource("BIM_Master.Icons.IconDeleteMaterial32.png");
            button.LargeImage = image;

            var button2 = new PushButtonData(
                "Ненужные параметры",
                "Ненужные \n параметры",
                pluginPath + "\\BIM_Master.dll",
                "DeleteParam.UnusedFamilyParametersCommand");

            BitmapImage image2 = LoadImageFromResource("BIM_Master.Icons.DeleteParam.png");
            button2.LargeImage = image2;

            var button3 = new PushButtonData(
                "Подсказки",
                "Подсказки",
                pluginPath + "\\BIM_Master.dll",
                "FamilyParameterEditor.Command");

            BitmapImage image3 = LoadImageFromResource("BIM_Master.Icons.Tip.png");
            button3.LargeImage = image3;

            var button4 = new PushButtonData(
               "Ненужные параметры",
               "ТестНенужные \n параметры",
               pluginPath + "\\BIM_Master.dll",
               "UnusedFamilyParametersCommand1");

            // Добавляем кнопки на панель
            panel.AddItem(button);
            panel.AddItem(button2);
            panel.AddItem(button3);
            panel2.AddItem(button4);

            return Result.Succeeded;
        }

        // Метод для загрузки изображения из ресурсов
        private BitmapImage LoadImageFromResource(string resourceName)
        {
            // Получаем текущую сборку
            var assembly = Assembly.GetExecutingAssembly();

            // Получаем поток ресурса из сборки
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    // Преобразуем поток в изображение
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }

            return null;
        }
    }
}
