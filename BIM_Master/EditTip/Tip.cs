using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;



namespace FamilyParameterEditor
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                if (!doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Ошибка", "Этот инструмент работает только в семействах!");
                    return Result.Cancelled;
                }

                FamilyManager familyManager = doc.FamilyManager;
                List<FamilyParameter> builtInParams = GetBuiltInFamilyParameters(doc); // Получаем список встроенных параметров
                List<ParameterDataItem> parametersData = familyManager.Parameters
                    .Cast<FamilyParameter>()
                    .Where(p => !builtInParams.Contains(p)) // Исключаем встроенные параметры
                    .Select(p => new ParameterDataItem(p, familyManager))
                    .OrderBy(p => p.Group)
                    .ThenBy(p => p.Name)
                    .ToList();

                using (ParametersForm form = new ParametersForm(parametersData))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        using (Transaction t = new Transaction(doc, "Обновление описаний параметров"))
                        {
                            t.Start();
                            foreach (var item in form.ParametersData)
                            {
                                if (!string.IsNullOrEmpty(item.Description) && !item.IsShared)
                                {
                                    familyManager.SetDescription(item.Parameter, item.Description);
                                }
                            }
                            t.Commit();
                        }
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public static List<FamilyParameter> GetBuiltInFamilyParameters(Document doc)
        {
            List<FamilyParameter> builtInParams = new List<FamilyParameter>();

            // Проверяем, что документ является файлом семейства
            if (!doc.IsFamilyDocument)
            {
                return builtInParams; // Возвращаем пустой список, если это не семейство
            }

            FamilyManager familyManager = doc.FamilyManager;

            foreach (FamilyParameter param in familyManager.Parameters)
            {
                // Фильтруем только встроенные параметры
                if (!param.IsShared && param.Definition is InternalDefinition internalDef &&
                    internalDef.BuiltInParameter != BuiltInParameter.INVALID)
                {
                    builtInParams.Add(param);
                }
            }

            return builtInParams;
        }
    }



    public class ParameterDataItem
    {
        public FamilyParameter Parameter { get; private set; }
        public string Name => Parameter.Definition.Name;
        public bool IsShared => Parameter.IsShared;
        public string Formula => Parameter.IsDeterminedByFormula ? Parameter.Formula : "";
        public string Group => LabelUtils.GetLabelFor(Parameter.Definition.ParameterGroup); // Название группы

        public string Description { get; set; }
        public string UsedInFormulas { get; private set; } // Новое поле

        public ParameterDataItem(FamilyParameter parameter, FamilyManager familyManager)
        {
            Parameter = parameter;
            Description = "";
            UsedInFormulas = FindUsageInFormulas(parameter, familyManager);
        }

        /// <summary>
        /// Находит параметры, в формулах которых используется текущий параметр.
        /// </summary>
        private string FindUsageInFormulas(FamilyParameter parameter, FamilyManager familyManager)
        {
            List<string> usedIn = new List<string>();
            string paramName = parameter.Definition.Name;

            foreach (FamilyParameter param in familyManager.Parameters)
            {
                if (param.IsDeterminedByFormula && param.Formula != null && param.Formula.Contains(paramName))
                {
                    usedIn.Add(param.Definition.Name);
                }
            }

            return usedIn.Any() ? string.Join(", ", usedIn) : "-";
        }
    }

    public class ParametersForm : System.Windows.Forms.Form
    {
        public List<ParameterDataItem> ParametersData { get; private set; }
        private DataGridView dataGridView;
        private Button btnSave;
        private Dictionary<string, bool> groupVisibility = new Dictionary<string, bool>(); // Состояние скрытия групп

        public ParametersForm(List<ParameterDataItem> parameters)
        {
            ParametersData = parameters;
            InitializeComponents();
            SetupGridView();
        }

        private void InitializeComponents()
        {
            this.ClientSize = new System.Drawing.Size(1200, 900);
            this.Text = "Редактор параметров семейства";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.White;
            this.Padding = new Padding(10);
            this.Font = new Font("Segoe UI", 10); // Увеличен общий шрифт

            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True },
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                BackgroundColor = System.Drawing.Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10) // Увеличен шрифт таблицы
            };

            // Увеличиваем высоту заголовков
            dataGridView.ColumnHeadersHeight = 40;

            btnSave = new Button
            {
                Text = "Сохранить",
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(0, 122, 204),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(dataGridView);
            this.Controls.Add(btnSave);
        }

        private void SetupGridView()
        {
            DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Имя параметра",
                ReadOnly = true,
                Width = 250
            };

            DataGridViewTextBoxColumn descColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Description",
                HeaderText = "Подсказка параметра",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 300
            };

            DataGridViewTextBoxColumn formulaColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Formula",
                HeaderText = "Формула",
                ReadOnly = true,
                Width = 250
            };

            DataGridViewTextBoxColumn usedInFormulaColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "UsedInFormulas",
                HeaderText = "Используется в формулах",
                ReadOnly = true,
                Width = 250
            };

            dataGridView.Columns.AddRange(nameColumn, descColumn, formulaColumn, usedInFormulaColumn);
            dataGridView.CellFormatting += DataGridView_CellFormatting;
            dataGridView.CellClick += DataGridView_CellClick;
            dataGridView.CellEndEdit += DataGridView_CellEndEdit;

            LoadData();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        // Обработчик клика по заголовку группы (скрытие/показ параметров)
        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView.Rows[e.RowIndex].Tag?.ToString() == "Group")
            {
                string groupName = dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString();
                groupVisibility[groupName] = !groupVisibility[groupName];

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Tag?.ToString() == groupName)
                    {
                        row.Visible = groupVisibility[groupName];
                    }
                }
            }
        }

        // Обработчик завершения редактирования, чтобы сохранить описание
        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridView.Rows[e.RowIndex].Tag?.ToString() != "Group")
            {
                string groupName = dataGridView.Rows[e.RowIndex].Tag?.ToString();
                var paramItem = ParametersData.FirstOrDefault(p => p.Name == dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString() && p.Group == groupName);

                if (paramItem != null)
                {
                    paramItem.Description = dataGridView.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
                }
            }
        }

        // Форматирование строк в таблице
        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView.Rows[e.RowIndex].Tag?.ToString() == "Group")
            {
                // Делаем заголовки групп синими и жирными
                e.CellStyle.ForeColor = System.Drawing.Color.DarkBlue;
                e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
            }
            else
            {
                ParameterDataItem param = ParametersData.FirstOrDefault(p => p.Name == dataGridView.Rows[e.RowIndex].Cells[0].Value.ToString());

                if (param != null && param.IsShared)
                {
                    // Если параметр общий, делаем текст слегка серым, но оставляем белый фон
                    e.CellStyle.ForeColor = System.Drawing.Color.Gray;
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Regular);
                    e.CellStyle.BackColor = System.Drawing.Color.White; // Фон белый
                }
            }
        }

        // Загрузка данных и группировка параметров
        private void LoadData()
        {
            dataGridView.Rows.Clear();
            groupVisibility.Clear();

            var groupedData = ParametersData.GroupBy(p => p.Group);

            foreach (var group in groupedData)
            {
                int rowIndex = dataGridView.Rows.Add(group.Key, "", "", ""); // Заголовок группы
                dataGridView.Rows[rowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
                dataGridView.Rows[rowIndex].DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
                dataGridView.Rows[rowIndex].Tag = "Group";

                groupVisibility[group.Key] = true;

                foreach (var param in group)
                {
                    int paramRow = dataGridView.Rows.Add(param.Name, param.Description, param.Formula, param.UsedInFormulas);
                    dataGridView.Rows[paramRow].Tag = group.Key;

                    if (param.IsShared)
                    {
                        dataGridView.Rows[paramRow].DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
                        dataGridView.Rows[paramRow].DefaultCellStyle.BackColor = System.Drawing.Color.White;
                    }
                }
            }


        }

    }

}
