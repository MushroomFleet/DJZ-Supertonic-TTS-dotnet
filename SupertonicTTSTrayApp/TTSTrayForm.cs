using System;
using System.Drawing;
using System.Windows.Forms;
using SupertonicTTSTrayApp.Services;

namespace SupertonicTTSTrayApp
{
    public partial class TTSTrayForm : Form
    {
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _trayMenu;
        private TextBox? _textInput;
        private Button? _generateButton;
        private ComboBox? _voiceComboBox;
        private Label? _statusLabel;
        private ProgressBar? _progressBar;
        private Button? _retryButton;
        private TextBox? _logTextBox;
        private SupertonicTTSService? _ttsService;
        private bool _isInitializing = false;
        private Button? _openFolderButton;
        private Button? _playbackButton;
        private string? _lastGeneratedFile;
        private TrackBar? _speedTrackBar;
        private Label? _speedValueLabel;

        public TTSTrayForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeControls();
            RedirectConsoleToLog();
            InitializeTTSAsync();
        }

        private void RedirectConsoleToLog()
        {
            var writer = new LogTextWriter(this);
            Console.SetOut(writer);
        }

        private class LogTextWriter : System.IO.TextWriter
        {
            private readonly TTSTrayForm _form;

            public LogTextWriter(TTSTrayForm form)
            {
                _form = form;
            }

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void WriteLine(string? value)
            {
                if (!string.IsNullOrEmpty(value))
                    _form.LogMessage(value);
            }

            public override void Write(string? value)
            {
                // Ignore non-line writes for cleaner output
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Supertonic TTS";
            this.Size = new Size(500, 595);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ShowInTaskbar = true;
        }

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show", null, OnShow);
            _trayMenu.Items.Add("Exit", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Text = "Supertonic TTS",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };

            // Create a simple icon (you can replace with a custom icon file)
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.DoubleClick += OnShow;
        }

        private void InitializeControls()
        {
            // Status Label
            _statusLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(440, 30),
                Text = "Initializing TTS...",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(_statusLabel);

            // Progress Bar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 55),
                Size = new Size(340, 25),
                Visible = true
            };
            this.Controls.Add(_progressBar);

            // Retry Button
            _retryButton = new Button
            {
                Location = new Point(370, 55),
                Size = new Size(90, 25),
                Text = "Retry",
                Enabled = false,
                Visible = false
            };
            _retryButton.Click += (s, e) => InitializeTTSAsync();
            this.Controls.Add(_retryButton);

            // Voice Selection Label
            var voiceLabel = new Label
            {
                Location = new Point(20, 95),
                Size = new Size(100, 25),
                Text = "Voice:",
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(voiceLabel);

            // Voice ComboBox
            _voiceComboBox = new ComboBox
            {
                Location = new Point(120, 92),
                Size = new Size(340, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            _voiceComboBox.Items.AddRange(new object[]
            {
                "Male 1",
                "Male 2",
                "Female 1",
                "Female 2"
            });
            _voiceComboBox.SelectedIndex = 0;
            this.Controls.Add(_voiceComboBox);

            // Speed Label
            var speedLabel = new Label
            {
                Location = new Point(20, 130),
                Size = new Size(100, 25),
                Text = "Speed:",
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(speedLabel);

            // Speed Value Label
            _speedValueLabel = new Label
            {
                Location = new Point(400, 130),
                Size = new Size(60, 25),
                Text = "1.0x",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(_speedValueLabel);

            // Speed TrackBar
            _speedTrackBar = new TrackBar
            {
                Location = new Point(120, 125),
                Size = new Size(270, 45),
                Minimum = 80,  // 0.8x speed
                Maximum = 160, // 1.6x speed
                Value = 100,   // 1.0x speed (default)
                TickFrequency = 10,
                Enabled = false
            };
            _speedTrackBar.ValueChanged += OnSpeedChanged;
            this.Controls.Add(_speedTrackBar);

            // Text Input Label
            var textLabel = new Label
            {
                Location = new Point(20, 175),
                Size = new Size(440, 25),
                Text = "Enter text to convert to speech:",
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(textLabel);

            // Text Input
            _textInput = new TextBox
            {
                Location = new Point(20, 205),
                Size = new Size(440, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Enabled = false,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(_textInput);

            // Generate Button
            _generateButton = new Button
            {
                Location = new Point(20, 320),
                Size = new Size(340, 40),
                Text = "Generate Speech",
                Enabled = false,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateButton.FlatAppearance.BorderSize = 0;
            _generateButton.Click += OnGenerateClick;
            this.Controls.Add(_generateButton);

            // Open Folder Button
            _openFolderButton = new Button
            {
                Location = new Point(370, 320),
                Size = new Size(40, 40),
                Text = "📁",
                Enabled = true,
                Font = new Font("Segoe UI", 16F),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _openFolderButton.FlatAppearance.BorderSize = 0;
            _openFolderButton.Click += OnOpenFolderClick;
            this.Controls.Add(_openFolderButton);

            // Playback Button
            _playbackButton = new Button
            {
                Location = new Point(420, 320),
                Size = new Size(40, 40),
                Text = "▶",
                Enabled = false,
                Font = new Font("Segoe UI", 14F),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _playbackButton.FlatAppearance.BorderSize = 0;
            _playbackButton.Click += OnPlaybackClick;
            this.Controls.Add(_playbackButton);

            // Info Label
            var infoLabel = new Label
            {
                Location = new Point(20, 370),
                Size = new Size(440, 20),
                Text = "Generated WAV files will be saved to Documents/DJZ-Supertonic/",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            this.Controls.Add(infoLabel);

            // Log Label
            var logLabel = new Label
            {
                Location = new Point(20, 400),
                Size = new Size(440, 20),
                Text = "Initialization Log:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(logLabel);

            // Log TextBox
            _logTextBox = new TextBox
            {
                Location = new Point(20, 425),
                Size = new Size(440, 130),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8F),
                BackColor = Color.White
            };
            this.Controls.Add(_logTextBox);
        }

        private void LogMessage(string message)
        {
            if (_logTextBox != null)
            {
                if (_logTextBox.InvokeRequired)
                {
                    _logTextBox.Invoke(new Action(() =>
                    {
                        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                    }));
                }
                else
                {
                    _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }
            }
        }

        private async void InitializeTTSAsync()
        {
            if (_isInitializing) return;
            _isInitializing = true;

            // Reset UI
            if (_retryButton != null)
            {
                _retryButton.Enabled = false;
                _retryButton.Visible = false;
            }
            if (_progressBar != null)
                _progressBar.Visible = true;

            try
            {
                LogMessage("Starting TTS initialization...");

                if (_statusLabel != null)
                    _statusLabel.Text = "Initializing TTS...";

                var modelsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SupertonicTTS", "Models");

                LogMessage($"Models directory: {modelsPath}");

                _ttsService = new SupertonicTTSService();

                _ttsService.DownloadProgress += (s, e) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            if (_statusLabel != null)
                                _statusLabel.Text = $"Downloading: {e.FileName} ({e.PercentComplete}%)";
                            if (_progressBar != null)
                                _progressBar.Value = Math.Min(e.PercentComplete, 100);

                            LogMessage($"Download progress: {e.FileName} - {e.PercentComplete}% ({e.CurrentFile}/{e.TotalFiles})");
                        }));
                    }
                };

                LogMessage("Calling InitializeAsync...");
                var initialized = await _ttsService.InitializeAsync();
                LogMessage($"InitializeAsync returned: {initialized}");

                if (initialized)
                {
                    LogMessage("TTS initialization successful!");

                    if (_statusLabel != null)
                        _statusLabel.Text = "Ready to generate speech!";
                    if (_progressBar != null)
                        _progressBar.Visible = false;
                    if (_generateButton != null)
                        _generateButton.Enabled = true;
                    if (_textInput != null)
                        _textInput.Enabled = true;
                    if (_voiceComboBox != null)
                        _voiceComboBox.Enabled = true;
                    if (_speedTrackBar != null)
                        _speedTrackBar.Enabled = true;
                }
                else
                {
                    LogMessage("ERROR: Initialization returned false");
                    LogMessage("Check internet connection and firewall settings");
                    LogMessage("Models must be downloaded from Hugging Face");

                    if (_statusLabel != null)
                        _statusLabel.Text = "Initialization failed - see log below";
                    if (_progressBar != null)
                        _progressBar.Visible = false;
                    if (_retryButton != null)
                    {
                        _retryButton.Enabled = true;
                        _retryButton.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                }

                MessageBox.Show(
                    $"Error initializing TTS:\n\n{ex.Message}\n\nCheck the log for details.",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                if (_statusLabel != null)
                    _statusLabel.Text = "Initialization failed - see log below";
                if (_progressBar != null)
                    _progressBar.Visible = false;
                if (_retryButton != null)
                {
                    _retryButton.Enabled = true;
                    _retryButton.Visible = true;
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async void OnGenerateClick(object? sender, EventArgs e)
        {
            if (_ttsService == null || _textInput == null || string.IsNullOrWhiteSpace(_textInput.Text))
            {
                MessageBox.Show("Please enter some text to convert to speech.", "No Text",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Disable controls during generation
                if (_generateButton != null)
                    _generateButton.Enabled = false;
                if (_statusLabel != null)
                    _statusLabel.Text = "Generating speech...";

                // Load selected voice
                var voiceStyle = _voiceComboBox?.SelectedIndex switch
                {
                    0 => VoiceStyle.Male1,
                    1 => VoiceStyle.Male2,
                    2 => VoiceStyle.Female1,
                    3 => VoiceStyle.Female2,
                    _ => VoiceStyle.Male1
                };

                await _ttsService.LoadVoiceStyleAsync(voiceStyle);

                // Generate output filename in DJZ-Supertonic subfolder
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var outputFolder = Path.Combine(documentsPath, "DJZ-Supertonic");

                // Create directory if it doesn't exist
                Directory.CreateDirectory(outputFolder);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputFile = Path.Combine(outputFolder, $"supertonic_tts_{timestamp}.wav");

                // Get speed value from trackbar
                var speed = _speedTrackBar != null ? _speedTrackBar.Value / 100f : 1.0f;
                var options = new TTSSynthesisOptions { Speed = speed };

                // Synthesize
                await _ttsService.SynthesizeToFileAsync(_textInput.Text, outputFile, options);

                // Store last generated file and enable playback button
                _lastGeneratedFile = outputFile;
                if (_playbackButton != null)
                    _playbackButton.Enabled = true;

                // Show success silently in status label
                if (_statusLabel != null)
                    _statusLabel.Text = "Speech generated successfully!";
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR generating speech: {ex.GetType().Name}: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LogMessage($"Inner exception: {ex.InnerException.Message}");
                    LogMessage($"Inner stack trace: {ex.InnerException.StackTrace}");
                }

                MessageBox.Show($"Error generating speech: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (_statusLabel != null)
                    _statusLabel.Text = "Error generating speech.";
            }
            finally
            {
                // Re-enable controls
                if (_generateButton != null)
                    _generateButton.Enabled = true;
                if (_statusLabel != null && _statusLabel.Text.StartsWith("Error") == false)
                    _statusLabel.Text = "Ready to generate speech!";
            }
        }

        private void OnSpeedChanged(object? sender, EventArgs e)
        {
            if (_speedTrackBar != null && _speedValueLabel != null)
            {
                var speed = _speedTrackBar.Value / 100f;
                _speedValueLabel.Text = $"{speed:F1}x";
            }
        }

        private void OnOpenFolderClick(object? sender, EventArgs e)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var outputFolder = Path.Combine(documentsPath, "DJZ-Supertonic");

                // Create directory if it doesn't exist
                Directory.CreateDirectory(outputFolder);

                // Open the folder in Explorer
                System.Diagnostics.Process.Start("explorer.exe", outputFolder);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR opening folder: {ex.Message}");
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnPlaybackClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedFile) || !File.Exists(_lastGeneratedFile))
            {
                MessageBox.Show("No audio file available for playback.", "Playback Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (_playbackButton != null)
                    _playbackButton.Enabled = false;
                return;
            }

            try
            {
                // Use default system player to play the audio file
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _lastGeneratedFile,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR playing audio: {ex.Message}");
                MessageBox.Show($"Error playing audio: {ex.Message}", "Playback Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnShow(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _trayIcon?.Dispose();
            _ttsService?.Dispose();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _trayMenu?.Dispose();
                _ttsService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
