// AudioPlayer.cs - Аудиоплеер на C# (WinForms + NAudio)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

public class AudioPlayer : Form
{
    private List<string> playlist = new List<string>();
    private int currentIndex = -1;
    private WaveOutEvent waveOut;
    private AudioFileReader audioFile;
    private bool isPlaying = false;
    private bool isPaused = false;
    private Timer timer;

    private Label titleLabel, timeLabel;
    private Button playBtn, stopBtn, prevBtn, nextBtn;
    private TrackBar volumeSlider;
    private ProgressBar progressBar;
    private ListBox playlistBox;
    private Label statusLabel;

    public AudioPlayer()
    {
        Text = "🎵 AudioForge - C#";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        waveOut = new WaveOutEvent();

        InitUI();
        InitEvents();
        timer = new Timer { Interval = 100 };
        timer.Tick += (s, e) => UpdateProgress();
        timer.Start();
    }

    private void InitUI()
    {
        BackColor = Color.FromArgb(44, 62, 80);

        // Заголовок
        Label title = new Label
        {
            Text = "🎵 AudioForge",
            Font = new Font("Arial", 18, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(44, 62, 80)
        };
        Controls.Add(title);

        // Панель управления
        FlowLayoutPanel controlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(236, 240, 241),
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 10, 10, 10)
        };
        playBtn = new Button { Text = "▶", Font = new Font("Arial", 12, FontStyle.Bold), Width = 60, Height = 40 };
        stopBtn = new Button { Text = "⏹", Font = new Font("Arial", 12, FontStyle.Bold), Width = 60, Height = 40 };
        prevBtn = new Button { Text = "⏮", Font = new Font("Arial", 12, FontStyle.Bold), Width = 60, Height = 40 };
        nextBtn = new Button { Text = "⏭", Font = new Font("Arial", 12, FontStyle.Bold), Width = 60, Height = 40 };
        controlPanel.Controls.Add(playBtn);
        controlPanel.Controls.Add(stopBtn);
        controlPanel.Controls.Add(prevBtn);
        controlPanel.Controls.Add(nextBtn);
        controlPanel.Controls.Add(new Label { Text = "🔊", AutoSize = true });
        volumeSlider = new TrackBar { Minimum = 0, Maximum = 100, Value = 80, Width = 100, Height = 30 };
        controlPanel.Controls.Add(volumeSlider);
        Controls.Add(controlPanel);

        // Информация
        TableLayoutPanel infoPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(248, 249, 250),
            RowCount = 2,
            ColumnCount = 1
        };
        titleLabel = new Label { Text = "Нет трека", Font = new Font("Arial", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
        timeLabel = new Label { Text = "00:00 / 00:00", Font = new Font("Arial", 10), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
        infoPanel.Controls.Add(titleLabel, 0, 0);
        infoPanel.Controls.Add(timeLabel, 0, 1);
        Controls.Add(infoPanel);

        // Прогресс
        progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 6, Style = ProgressBarStyle.Continuous };
        Controls.Add(progressBar);

        // Плейлист
        Panel playlistPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        playlistBox = new ListBox { Dock = DockStyle.Fill, Font = new Font("Arial", 10) };
        playlistBox.DoubleClick += (s, e) => {
            int idx = playlistBox.SelectedIndex;
            if (idx >= 0) { currentIndex = idx; Stop(); PlayTrack(); }
        };
        playlistPanel.Controls.Add(playlistBox);

        FlowLayoutPanel playlistControls = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };
        Button addBtn = new Button { Text = "➕ Добавить", BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        addBtn.Click += (s, e) => AddFiles();
        playlistControls.Controls.Add(addBtn);
        Button removeBtn = new Button { Text = "🗑 Удалить", BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        removeBtn.Click += (s, e) => RemoveSelected();
        playlistControls.Controls.Add(removeBtn);
        Button clearBtn = new Button { Text = "🔄 Очистить", BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        clearBtn.Click += (s, e) => ClearPlaylist();
        playlistControls.Controls.Add(clearBtn);
        playlistPanel.Controls.Add(playlistControls);

        Controls.Add(playlistPanel);

        // Статус
        statusLabel = new Label
        {
            Text = "Готов",
            ForeColor = Color.Gray,
            Dock = DockStyle.Bottom,
            Height = 25,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 9)
        };
        Controls.Add(statusLabel);

        playBtn.Click += (s, e) => PlayPause();
        stopBtn.Click += (s, e) => Stop();
        prevBtn.Click += (s, e) => PrevTrack();
        nextBtn.Click += (s, e) => NextTrack();
        volumeSlider.ValueChanged += (s, e) => {
            if (waveOut != null) waveOut.Volume = volumeSlider.Value / 100.0f;
        };
    }

    private void InitEvents()
    {
        waveOut.PlaybackStopped += (s, e) => {
            isPlaying = false;
            Invoke(new Action(() => {
                playBtn.Text = "▶";
                statusLabel.Text = "Завершено";
                if (playlist.Count > 0) NextTrack();
            }));
        };
    }

    private void AddFiles()
    {
        OpenFileDialog ofd = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "MP3 files|*.mp3|All files|*.*"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            foreach (string file in ofd.FileNames)
            {
                playlist.Add(file);
                playlistBox.Items.Add(Path.GetFileName(file));
            }
            if (currentIndex == -1 && playlist.Count > 0)
            {
                currentIndex = 0;
                LoadTrack(currentIndex);
            }
        }
    }

    private void RemoveSelected()
    {
        int idx = playlistBox.SelectedIndex;
        if (idx < 0) return;
        playlist.RemoveAt(idx);
        playlistBox.Items.RemoveAt(idx);
        if (currentIndex == idx) { Stop(); currentIndex = -1; titleLabel.Text = "Нет трека"; timeLabel.Text = "00:00 / 00:00"; progressBar.Value = 0; }
        else if (currentIndex > idx) currentIndex--;
    }

    private void ClearPlaylist()
    {
        playlist.Clear();
        playlistBox.Items.Clear();
        Stop();
        currentIndex = -1;
        titleLabel.Text = "Нет трека";
        timeLabel.Text = "00:00 / 00:00";
        progressBar.Value = 0;
    }

    private void LoadTrack(int index)
    {
        if (index < 0 || index >= playlist.Count) return;
        string file = playlist[index];
        titleLabel.Text = Path.GetFileName(file);
        timeLabel.Text = "00:00 / 00:00";
        progressBar.Value = 0;
    }

    private void PlayTrack()
    {
        if (currentIndex < 0 || currentIndex >= playlist.Count) return;
        Stop();
        try
        {
            audioFile = new AudioFileReader(playlist[currentIndex]);
            waveOut.Init(audioFile);
            waveOut.Volume = volumeSlider.Value / 100.0f;
            waveOut.Play();
            isPlaying = true;
            isPaused = false;
            playBtn.Text = "⏸";
            statusLabel.Text = "Играет";
            // Обновление времени
            timeLabel.Text = $"00:00 / {FormatTime(audioFile.TotalTime.TotalSeconds)}";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Ошибка: " + ex.Message;
        }
    }

    private void PlayPause()
    {
        if (playlist.Count == 0) { statusLabel.Text = "Нет треков в плейлисте"; return; }
        if (currentIndex == -1) { currentIndex = 0; LoadTrack(0); PlayTrack(); return; }

        if (isPlaying && !isPaused)
        {
            waveOut.Pause();
            isPaused = true;
            playBtn.Text = "▶";
            statusLabel.Text = "Пауза";
        }
        else if (isPaused)
        {
            waveOut.Play();
            isPaused = false;
            playBtn.Text = "⏸";
            statusLabel.Text = "Играет";
        }
        else
        {
            PlayTrack();
        }
    }

    private void Stop()
    {
        waveOut.Stop();
        isPlaying = false;
        isPaused = false;
        playBtn.Text = "▶";
        statusLabel.Text = "Остановлено";
        progressBar.Value = 0;
        if (audioFile != null) { audioFile.Position = 0; }
    }

    private void NextTrack()
    {
        if (playlist.Count == 0) return;
        currentIndex = (currentIndex + 1) % playlist.Count;
        Stop();
        LoadTrack(currentIndex);
        PlayTrack();
    }

    private void PrevTrack()
    {
        if (playlist.Count == 0) return;
        currentIndex = (currentIndex - 1 + playlist.Count) % playlist.Count;
        Stop();
        LoadTrack(currentIndex);
        PlayTrack();
    }

    private void UpdateProgress()
    {
        if (isPlaying && !isPaused && audioFile != null)
        {
            double total = audioFile.TotalTime.TotalSeconds;
            double pos = audioFile.CurrentTime.TotalSeconds;
            if (total > 0)
            {
                progressBar.Value = (int)((pos / total) * 100);
                timeLabel.Text = $"{FormatTime(pos)} / {FormatTime(total)}";
            }
        }
    }

    private string FormatTime(double seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m:D2}:{s:D2}";
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new AudioPlayer());
    }
}
