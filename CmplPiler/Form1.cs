using System.Diagnostics;
using System.Threading.Tasks;

namespace CmplPiler
{
    public partial class Form1 : Form
    {
        private CmplProject _currentProject;
        public Form1()
        {
            try
            {
                InitializeComponent();
                this.Load += Form1_Load;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}\n\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Log("Application started successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on form load: {ex.Message}", "Load Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.Filter = "Cmpl files (*.cmpl)|*.cmpl|All files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentProject = CmplParser.LoadFile(openFileDialog.FileName);
                        this.Text = $"Cmpl Builder - {_currentProject.project_name}";

                        cmbProfiles.Items.Clear();
                        foreach (var profile in _currentProject.profiles)
                        {
                            cmbProfiles.Items.Add(profile.name);
                        }
                        if (cmbProfiles.Items.Count > 0)
                        {
                            cmbProfiles.SelectedIndex = 0;
                        }

                        Log("Project loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Log($"Error loading project: {ex.Message}");
                    }
                }
            }
        }

        private async void btnBuild_Click(object sender, EventArgs e)
        {
            if (_currentProject == null || cmbProfiles.SelectedIndex == -1) return;

            string selectedProfileName = cmbProfiles.SelectedItem.ToString();
            CmplProfile selectedProfile = _currentProject.profiles.Find(p => p.name == selectedProfileName);

            var tasks = CommandGenerator.GenerateTasks(_currentProject, selectedProfile);

            btnBuild.Enabled = false;
            txtOutput.Clear();
            Log($"--- Starting Build Profile: {selectedProfile.name} ---");

            foreach (var task in tasks)
            {
                int exitCode = await RunProcessAsync(task.Command, task.Arguments);

                if (exitCode != 0)
                {
                    Log($"\n[ERROR] Task failed with exit code {exitCode}. Stopping build.");
                    break;
                }
            }

            Log("\n--- Build Sequence Completed ---");
            btnBuild.Enabled = true;
        }

        private Task<int> RunProcessAsync(string command, string arguments)
        {
            var tcs = new TaskCompletionSource<int>();

            Log($"\n> {command} {arguments}");

            ProcessStartInfo startInfo = new()
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            Process process = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, ev) => { if (ev.Data != null) Log(ev.Data); };
            process.ErrorDataReceived += (s, ev) => { if (ev.Data != null) Log($"ERROR: {ev.Data}"); };

            process.Exited += (s, ev) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private void RunProcess(string command, string arguments)
        {
            Log($"\n--- Running: {command} {arguments} ---");

            ProcessStartInfo startInfo = new()
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process process = new() { StartInfo = startInfo };

            process.OutputDataReceived += (s, ev) => { if (ev.Data != null) Log(ev.Data); };
            process.ErrorDataReceived += (s, ev) => { if (ev.Data != null) Log($"ERROR: {ev.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void Log(string message)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(() =>
                {
                    txtOutput.AppendText(message + Environment.NewLine);
                }));

            }
            else
            {
                txtOutput.AppendText(message + Environment.NewLine);
            }
        }
    }
}
