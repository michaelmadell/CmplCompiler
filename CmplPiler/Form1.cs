using CmplPiler.Core;

namespace CmplPiler
{
    public partial class Form1 : Form
    {
        private CmplProject? _currentProject;
        private CancellationTokenSource? _buildCts;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Cmpl files (*.cmpl)|*.cmpl|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                _currentProject = CmplParser.LoadFile(openFileDialog.FileName);
                this.Text = $"Cmpl Builder - {_currentProject.ProjectName}";

                cmbProfiles.Items.Clear();
                foreach (var profile in _currentProject.Profiles)
                {
                    cmbProfiles.Items.Add(profile.Name ?? "<unnamed>");
                }
                if (cmbProfiles.Items.Count > 0)
                {
                    cmbProfiles.SelectedIndex = 0;
                }

                Log($"Project '{_currentProject.ProjectName}' loaded ({_currentProject.Profiles.Count} profile(s))");
            }
            catch (Exception ex)
            {
                _currentProject = null;
                cmbProfiles.Items.Clear();
                Log($"Error loading project: {ex.Message}");
            }
        }

        private async void btnBuild_Click(object? sender, EventArgs e)
        {
            if (_currentProject == null || cmbProfiles.SelectedIndex == -1) return;

            string? selectedProfileName = cmbProfiles.SelectedItem?.ToString();
            CmplProfile? selectedProfile = _currentProject.Profiles.Find(p => p.Name == selectedProfileName);
            if (selectedProfile == null) return;

            btnBuild.Enabled = false;
            btnCancel.Enabled = true;
            txtOutput.Clear();
            Log($"--- Starting Build Profile: {selectedProfile.Name} ---");

            var runner = new BuildRunner();
            runner.OutputReceived += Log;
            runner.ErrorReceived += Log;

            _buildCts = new CancellationTokenSource();
            try
            {
                int exitCode = await runner.RunAsync(_currentProject, selectedProfile, _buildCts.Token);
                Log(exitCode == 0
                    ? "\n--- Build Sequence Completed ---"
                    : $"\n[ERROR] Build failed with exit code {exitCode}.");
            }
            catch (OperationCanceledException)
            {
                Log("\n--- Build Cancelled ---");
            }
            catch (Exception ex)
            {
                Log($"\n[ERROR] {ex.Message}");
            }
            finally
            {
                _buildCts.Dispose();
                _buildCts = null;
                btnBuild.Enabled = true;
                btnCancel.Enabled = false;
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            _buildCts?.Cancel();
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