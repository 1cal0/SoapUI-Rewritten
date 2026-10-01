using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SoapUI.Common;
using SoapUI.Models;
using SoapUI.Models.Responses;
using SoapUI.Services.IO;
using SoapUI.Services.Parsing;
using SoapUI.Services.Soap;

namespace SoapUI.UI
{
    /// <summary>
    /// Main application window. Thin orchestrator only:
    /// reads inputs, delegates to service clients, renders outcomes.
    /// SOAP construction, transport and XML parsing live in Services/.
    /// </summary>
    public partial class MainUI : Form
    {
        private const int ArgumentRowHeight = 33;

        private readonly SoapTransport _transport = new SoapTransport();
        private readonly ArgumentFileStore _argumentStore = new ArgumentFileStore();

        private int _argumentNextY = 4;

        public MainUI()
        {
            InitializeComponent();
            ConfigureBuildMetadata();
            logBox.Clear();
            logBox.Size = new Size(776, 85);
        }

        private void ConfigureBuildMetadata()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(MainUI).Assembly;
            var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            string createdBy = company != null && !string.IsNullOrWhiteSpace(company.Company)
                ? company.Company
                : "SoapUI contributors";

            string assemblyPath = assembly.Location;
            DateTime buildDate = !string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath)
                ? File.GetLastWriteTime(assemblyPath)
                : DateTime.Now;

            createdByLbl.Text = string.Format(
                "Created by {0} | Build: {1}",
                createdBy,
                buildDate.ToString("yyyy-MM-dd HH:mm"));
            repoLink.Text = AppConstants.RepositoryUrl;
            repoLink.Location = new Point(330, 428);
        }

        #region Inputs -> domain objects

        private ServiceMode CurrentMode
        {
            get { return rbxgsMode.Checked ? ServiceMode.Rbxgs : ServiceMode.Rcc; }
        }

        private string SelectedAction
        {
            get { return soapAction.GetItemText(soapAction.SelectedItem); }
        }

        private ServiceEndpoint BuildEndpoint()
        {
            int port = AppConstants.DefaultPort;
            if (!CurrentMode.Equals(ServiceMode.Rbxgs))
            {
                if (!int.TryParse(portText(), out port))
                {
                    throw new InvalidOperationException("Port must be a number.");
                }
            }

            return new ServiceEndpoint(ip.Text, port, baseUrl.Text, CurrentMode);
        }

        private string portText()
        {
            return port.Text;
        }

        private Job BuildOpenJob()
        {
            double expiration;
            if (!double.TryParse(openJobExpiration.Text, out expiration))
            {
                throw new InvalidOperationException("Open-job expiration must be a number.");
            }

            double cores;
            if (!double.TryParse(openJobCores.Text, out cores))
            {
                throw new InvalidOperationException("Core count must be a number.");
            }

            return new Job(openJobId.Text, expiration, openJobCategory.SelectedIndex, cores);
        }

        private Script BuildScript(string name, string content, string environmentId)
        {
            return new Script(name, content, GetLuaArguments(), environmentId);
        }

        private IList<LuaArgument> GetLuaArguments()
        {
            var list = new List<LuaArgument>();

            foreach (Control control in scriptArgumentPanel.Controls)
            {
                if (control.Name != "argumentTemplate" || !control.Visible)
                {
                    continue;
                }

                string type = control.Controls["argumentType"].Text;
                string value = control.Controls["argumentValue"].Text;
                list.Add(new LuaArgument(type, value));
            }

            return list;
        }

        #endregion

        #region Logging + outcome rendering

        public bool AddToLog(string logText)
        {
            logBox.AppendText(logText + "\r\n");
            return true;
        }

        private void AddLines(IEnumerable<string> lines)
        {
            if (lines == null)
            {
                return;
            }

            foreach (var line in lines)
            {
                AddToLog(line);
            }
        }

        private void ShowOutcome(ResponseOutcome outcome)
        {
            Guard.AgainstNull(outcome, nameof(outcome));

            AddLines(outcome.LogLines);

            if (outcome.HasDialog)
            {
                MessageBox.Show(this, outcome.DialogText, outcome.DialogTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region Execute dispatch

        private void executeBtn_Click(object sender, EventArgs e)
        {
            string action = SelectedAction;
            if (string.IsNullOrWhiteSpace(action))
            {
                MessageBox.Show(this, "Please select a SOAP action first.", "No action selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddToLog("Attempting to execute " + action);
            Enabled = false;

            try
            {
                ServiceEndpoint endpoint = BuildEndpoint();
                string rawXml = Dispatch(endpoint, action);
                AddToLog("Received a response from the server.");
                var parser = new SoapResponseParser(endpoint.Mode);
                ShowOutcome(parser.Parse(rawXml, action));
            }
            catch (SoapTransportException ex)
            {
                if (ex.HasHttpResponse)
                {
                    string status = ex.StatusCode.HasValue
                        ? " (HTTP " + ex.StatusCode.Value + ")"
                        : string.Empty;
                    AddToLog("Received a response from the server" + status + ".");
                }
                else
                {
                    AddToLog("No response received from the server.");
                }

                AddToLog("Transport error: " + ex.Message);
                string details = ex.Message;
                string title = "SOAP transport error";

                if (!string.IsNullOrWhiteSpace(ex.FaultString))
                {
                    string fault = string.IsNullOrWhiteSpace(ex.FaultCode)
                        ? ex.FaultString
                        : ex.FaultCode + ": " + ex.FaultString;
                    AddToLog("SOAP fault: " + fault);
                    details = "The RCC service rejected the request.\r\n\r\n" + fault;
                    title = "SOAP service fault";
                }
                else if (!string.IsNullOrEmpty(ex.ResponseBody))
                {
                    AddToLog("Server response: " + ex.ResponseBody);
                    details += "\r\n\r\nServer response:\r\n" + ex.ResponseBody;
                }

                MessageBox.Show(this, details, title,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AddToLog("Error whilst executing SOAPAction: " + ex.Message);
                MessageBox.Show(this, "Error: " + ex.Message, "Error whilst executing SOAPAction",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
            }
        }

        /// <summary>
        /// Routes the selected action to the matching service client.
        /// Each branch only gathers already-validated inputs.
        /// </summary>
        private string Dispatch(ServiceEndpoint endpoint, string action)
        {
            if (endpoint.IsRbxgs)
            {
                return DispatchRbxgs(new RbxgsServiceClient(endpoint, _transport), action);
            }

            return DispatchRcc(new RccServiceClient(endpoint, _transport), action);
        }

        private string DispatchRcc(RccServiceClient client, string action)
        {
            switch (action)
            {
                case "HelloWorld": return client.HelloWorld();
                case "GetVersion": return client.GetVersion();
                case "GetStatus": return client.GetStatus();
                case "OpenJobEx": return client.OpenJobEx(BuildOpenJob(), BuildScript("GameServer", openJobScript.Text, null));
                case "OpenJob": return client.OpenJob(BuildOpenJob(), BuildScript("GameServer", openJobScript.Text, null));
                case "RenewLease": return client.RenewLease(renewJobId.Text, ParseDouble(renewExpiration.Text, "Renewal expiration"));
                case "ExecuteEx": return client.ExecuteEx(executeJobId.Text, BuildScript("ExecuteEx", executeScript.Text, null));
                case "Execute": return client.Execute(executeJobId.Text, BuildScript("Execute", executeScript.Text, executeJobId.Text));
                case "CloseJob": return client.CloseJob(closeJobId.Text);
                case "DiagEx": return client.DiagEx(ParseInt(diagExType.Text, "Diag type"), diagExJobId.Text);
                case "Diag": return client.Diag(ParseInt(diagExType.Text, "Diag type"), diagExJobId.Text);
                case "GetAllJobsEx": return client.GetAllJobsEx();
                case "GetAllJobs": return client.GetAllJobs();
                case "CloseExpiredJobs": return client.CloseExpiredJobs();
                case "CloseAllJobs": return client.CloseAllJobs();
                default:
                    throw new InvalidOperationException("SOAP action '" + action + "' is not implemented for RCC.");
            }
        }

        private string DispatchRbxgs(RbxgsServiceClient client, string action)
        {
            switch (action)
            {
                case "HelloWorld": return client.HelloWorld();
                case "GetVersion": return client.GetVersion();
                case "GetStatus": return client.GetStatus();
                case "Execute": return client.Execute(BuildScript("Execute", executeScript.Text, executeJobId.Text));
                case "GetStandardOutMessages": return client.GetStandardOutMessages(30);
                case "GetAllEnvironments": return client.GetAllEnvironments();
                case "OpenEnvironment": return client.OpenEnvironment();
                case "CloseEnvironment": return client.CloseEnvironment(closeJobId.Text);
                case "CloseAllEnvironments": return client.CloseAllEnvironments();
                case "CloseOrphanedEnvironments": return client.CloseOrphanedEnvironments();
                case "Update": return client.Update(null);
                default:
                    throw new InvalidOperationException("SOAP action '" + action + "' is not implemented for RBXGS.");
            }
        }

        private static int ParseInt(string text, string fieldName)
        {
            int value;
            if (!int.TryParse(text, out value))
            {
                throw new InvalidOperationException(fieldName + " must be a number.");
            }

            return value;
        }

        private static double ParseDouble(string text, string fieldName)
        {
            double value;
            if (!double.TryParse(text, out value))
            {
                throw new InvalidOperationException(fieldName + " must be a number.");
            }

            return value;
        }

        #endregion

        #region Action panel visibility

        private void hideAllPanels()
        {
            noExtraInfoLbl.Visible = false;
            openJobPanel.Visible = false;
            renewJobPanel.Visible = false;
            executePanel.Visible = false;
            closeJobPanel.Visible = false;
            diagExPanel.Visible = false;
            scriptArgumentPanel.Visible = false;
            loadScriptBtn.Visible = false;
            logBox.Size = new Size(776, 85);
        }

        private void ShowScriptPanels()
        {
            scriptArgumentPanel.Visible = true;
            loadScriptBtn.Visible = true;
            logBox.Size = new Size(282, 85);
        }

        private void soapAction_SelectedValueChanged(object sender, EventArgs e)
        {
            hideAllPanels();

            switch (SelectedAction)
            {
                case "OpenJobEx":
                case "OpenJob":
                    openJobPanel.Visible = true;
                    ShowScriptPanels();
                    break;
                case "RenewLease":
                    renewJobPanel.Visible = true;
                    break;
                case "ExecuteEx":
                case "Execute":
                    executePanel.Visible = true;
                    ShowScriptPanels();
                    break;
                case "CloseJob":
                case "CloseEnvironment":
                    closeJobPanel.Visible = true;
                    break;
                case "DiagEx":
                case "Diag":
                    diagExPanel.Visible = true;
                    break;
                default:
                    noExtraInfoLbl.Visible = true;
                    break;
            }
        }

        #endregion

        #region Script argument rows

        private void addArgumentButton_Click(object sender, EventArgs e)
        {
            AddArgumentPanel(LuaType.Nil, string.Empty);
        }

        /// <summary>
        /// Creates a new argument row without instantiating a scratch form
        /// (the legacy code did `new MainUI().argumentTemplate`).
        /// </summary>
        private void AddArgumentPanel(string type, string value)
        {
            var row = new Panel
            {
                Name = "argumentTemplate",
                Size = argumentTemplate.Size,
                Location = new Point(4, _argumentNextY),
                Visible = true
            };

            var typeBox = new ComboBox
            {
                Name = "argumentType",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = argumentTemplate.Controls["argumentType"].Location,
                Size = argumentTemplate.Controls["argumentType"].Size
            };
            typeBox.Items.AddRange(AppConstants.SupportedLuaTypes);
            typeBox.Text = string.IsNullOrEmpty(type) ? LuaType.Nil : type;

            var valueBox = new TextBox
            {
                Name = "argumentValue",
                Location = argumentTemplate.Controls["argumentValue"].Location,
                Size = argumentTemplate.Controls["argumentValue"].Size,
                Text = value ?? string.Empty
            };

            var removeButton = new Button
            {
                Name = "removeArgument",
                Text = "Remove",
                Location = argumentTemplate.Controls["removeArgument"].Location,
                Size = argumentTemplate.Controls["removeArgument"].Size,
                UseVisualStyleBackColor = true
            };
            removeButton.Click += removeArgument_Click;

            row.Controls.Add(typeBox);
            row.Controls.Add(valueBox);
            row.Controls.Add(removeButton);

            scriptArgumentPanel.Controls.Add(row);

            scriptArgumentPanel.AutoScroll = false;
            scriptArgumentPanel.HorizontalScroll.Enabled = false;
            scriptArgumentPanel.HorizontalScroll.Visible = false;
            scriptArgumentPanel.HorizontalScroll.Maximum = 0;
            scriptArgumentPanel.AutoScroll = true;

            _argumentNextY += ArgumentRowHeight;
        }

        private void removeArgument_Click(object sender, EventArgs e)
        {
            var button = sender as Control;
            if (button == null || button.Parent == null)
            {
                return;
            }

            var row = button.Parent;
            int removedY = row.Location.Y;

            var host = row.Parent;
            host.Controls.Remove(row);
            row.Dispose();

            foreach (Control item in host.Controls)
            {
                if (item.Name == "argumentTemplate" && item.Visible && item.Location.Y > removedY)
                {
                    item.Location = new Point(item.Location.X, item.Location.Y - ArgumentRowHeight);
                }
            }

            _argumentNextY -= ArgumentRowHeight;
        }

        private void ClearArgumentRows()
        {
            var rows = new List<Control>();
            foreach (Control item in scriptArgumentPanel.Controls)
            {
                if (item.Name == "argumentTemplate" && item.Visible)
                {
                    rows.Add(item);
                }
            }

            foreach (var row in rows)
            {
                scriptArgumentPanel.Controls.Remove(row);
                row.Dispose();
            }

            _argumentNextY = 4;
        }

        #endregion

        #region File import / export

        private void openArgsFromFileBtn_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Argument files (*.json;*.xml;*.txt)|*.json;*.xml;*.txt|JSON files (*.json)|*.json|XML files (*.xml)|*.xml";
                dialog.Title = "Please pick a valid JSON or XML argument file";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                IList<LuaArgument> arguments;
                try
                {
                    arguments = _argumentStore.Load(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not load arguments: " + ex.Message, "Load arguments",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ClearArgumentRows();
                foreach (var argument in arguments)
                {
                    AddArgumentPanel(argument.Type, argument.ValueAsString);
                }
            }
        }

        private void saveArgsToFileBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string directory = Path.GetDirectoryName(Application.ExecutablePath);
                string fileName = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ".json";
                string fullPath = _argumentStore.Save(GetLuaArguments(), directory, fileName);
                AddToLog("Wrote arguments to " + fullPath + "!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save arguments: " + ex.Message, "Save arguments",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadScriptBtn_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|Lua files (*.lua)|*.lua";
                dialog.Title = "Please pick a valid text or lua file";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string file;
                try
                {
                    file = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not load script: " + ex.Message, "Load script",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                switch (SelectedAction)
                {
                    case "OpenJobEx":
                    case "OpenJob":
                        openJobScript.Text = file;
                        break;
                    case "ExecuteEx":
                    case "Execute":
                        executeScript.Text = file;
                        break;
                    default:
                        MessageBox.Show(this, "Invalid SOAPAction to load script for.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
        }

        #endregion

        #region Misc UI

        private void randomizeButton_Click(object sender, EventArgs e)
        {
            openJobId.Text = Guid.NewGuid().ToString("D").ToLowerInvariant();
        }

        private void repoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(AppConstants.RepositoryUrl);
                repoLink.LinkVisited = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open repository link: " + ex.Message, "Open link",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void rbxgsMode_CheckedChanged(object sender, EventArgs e)
        {
            hideAllPanels();
            soapAction.Items.Clear();

            string[] actions = rbxgsMode.Checked
                ? AppConstants.RbxgsSoapActions
                : AppConstants.RccSoapActions;

            foreach (string action in actions)
            {
                soapAction.Items.Add(action);
            }

            bool isRcc = !rbxgsMode.Checked;
            baseUrl.Visible = isRcc;
            baseUrlLbl.Visible = isRcc;
            port.Visible = isRcc;
            portLbl.Visible = isRcc;
        }

        #endregion
    }
}
