// AudioPlayer.java - Аудиоплеер на Java (Swing + JLayer)
import javax.swing.*;
import javax.swing.filechooser.FileNameExtensionFilter;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.util.*;
import java.util.List;
import javazoom.jl.player.Player;
import javazoom.jl.decoder.JavaLayerException;

public class AudioPlayer extends JFrame {
    private List<String> playlist = new ArrayList<>();
    private int currentIndex = -1;
    private boolean isPlaying = false;
    private boolean isPaused = false;
    private Player player;
    private Thread playThread;
    private float volume = 0.8f;

    private JLabel titleLabel, timeLabel;
    private JButton playBtn, stopBtn, prevBtn, nextBtn;
    private JSlider volumeSlider;
    private JProgressBar progressBar;
    private DefaultListModel<String> playlistModel;
    private JList<String> playlistList;
    private JLabel statusLabel;
    private Timer timer;

    public AudioPlayer() {
        setTitle("🎵 AudioForge - Java");
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setSize(600, 500);
        setLocationRelativeTo(null);
        setResizable(false);
        setLayout(new BorderLayout());

        // Верхняя панель
        JPanel top = new JPanel(new BorderLayout());
        top.setBackground(new Color(44, 62, 80));
        top.setPreferredSize(new Dimension(0, 50));
        JLabel title = new JLabel("🎵 AudioForge", SwingConstants.CENTER);
        title.setFont(new Font("Arial", Font.BOLD, 18));
        title.setForeground(Color.WHITE);
        top.add(title, BorderLayout.CENTER);
        add(top, BorderLayout.NORTH);

        // Управление
        JPanel controlPanel = new JPanel(new FlowLayout(FlowLayout.CENTER, 10, 10));
        controlPanel.setBackground(new Color(236, 240, 241));

        playBtn = new JButton("▶");
        playBtn.setFont(new Font("Arial", Font.BOLD, 16));
        playBtn.setPreferredSize(new Dimension(60, 40));
        playBtn.addActionListener(e -> playPause());
        controlPanel.add(playBtn);

        stopBtn = new JButton("⏹");
        stopBtn.setFont(new Font("Arial", Font.BOLD, 16));
        stopBtn.setPreferredSize(new Dimension(60, 40));
        stopBtn.addActionListener(e -> stop());
        controlPanel.add(stopBtn);

        prevBtn = new JButton("⏮");
        prevBtn.setFont(new Font("Arial", Font.BOLD, 16));
        prevBtn.setPreferredSize(new Dimension(60, 40));
        prevBtn.addActionListener(e -> prevTrack());
        controlPanel.add(prevBtn);

        nextBtn = new JButton("⏭");
        nextBtn.setFont(new Font("Arial", Font.BOLD, 16));
        nextBtn.setPreferredSize(new Dimension(60, 40));
        nextBtn.addActionListener(e -> nextTrack());
        controlPanel.add(nextBtn);

        controlPanel.add(new JLabel("🔊"));
        volumeSlider = new JSlider(0, 100, 80);
        volumeSlider.setPreferredSize(new Dimension(100, 30));
        volumeSlider.addChangeListener(e -> {
            volume = volumeSlider.getValue() / 100.0f;
            // JLayer не поддерживает громкость напрямую, но мы будем использовать для будущих реализаций
        });
        controlPanel.add(volumeSlider);

        add(controlPanel, BorderLayout.NORTH);

        // Информация
        JPanel infoPanel = new JPanel(new GridLayout(2, 1));
        infoPanel.setBackground(new Color(248, 249, 250));
        titleLabel = new JLabel("Нет трека", SwingConstants.CENTER);
        titleLabel.setFont(new Font("Arial", Font.BOLD, 14));
        infoPanel.add(titleLabel);
        timeLabel = new JLabel("00:00 / 00:00", SwingConstants.CENTER);
        timeLabel.setFont(new Font("Arial", Font.PLAIN, 12));
        infoPanel.add(timeLabel);
        add(infoPanel, BorderLayout.CENTER);

        // Прогресс
        progressBar = new JProgressBar(0, 100);
        progressBar.setStringPainted(false);
        progressBar.setPreferredSize(new Dimension(0, 6));
        add(progressBar, BorderLayout.SOUTH);

        // Плейлист
        JPanel playlistPanel = new JPanel(new BorderLayout());
        playlistPanel.setBorder(BorderFactory.createTitledBorder("📋 Плейлист"));
        playlistModel = new DefaultListModel<>();
        playlistList = new JList<>(playlistModel);
        playlistList.addMouseListener(new MouseAdapter() {
            public void mouseClicked(MouseEvent e) {
                if (e.getClickCount() == 2) {
                    int index = playlistList.getSelectedIndex();
                    if (index >= 0) {
                        currentIndex = index;
                        stop();
                        loadTrack(currentIndex);
                        playPause();
                    }
                }
            }
        });
        playlistPanel.add(new JScrollPane(playlistList), BorderLayout.CENTER);

        JPanel playlistControls = new JPanel(new FlowLayout(FlowLayout.LEFT));
        JButton addBtn = new JButton("➕ Добавить");
        addBtn.addActionListener(e -> addFiles());
        playlistControls.add(addBtn);
        JButton removeBtn = new JButton("🗑 Удалить");
        removeBtn.addActionListener(e -> removeSelected());
        playlistControls.add(removeBtn);
        JButton clearBtn = new JButton("🔄 Очистить");
        clearBtn.addActionListener(e -> clearPlaylist());
        playlistControls.add(clearBtn);
        playlistPanel.add(playlistControls, BorderLayout.SOUTH);

        add(playlistPanel, BorderLayout.WEST);

        // Статус
        statusLabel = new JLabel("Готов", SwingConstants.CENTER);
        statusLabel.setForeground(Color.GRAY);
        statusLabel.setFont(new Font("Arial", Font.PLAIN, 10));
        add(statusLabel, BorderLayout.AFTER_LAST_LINE);

        // Таймер обновления прогресса
        timer = new Timer(100, e -> updateProgress());
        timer.start();

        setVisible(true);
    }

    private void addFiles() {
        JFileChooser fc = new JFileChooser();
        fc.setMultiSelectionEnabled(true);
        fc.setFileFilter(new FileNameExtensionFilter("MP3 files", "mp3"));
        if (fc.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            for (File f : fc.getSelectedFiles()) {
                playlist.add(f.getAbsolutePath());
                playlistModel.addElement(f.getName());
            }
            if (currentIndex == -1 && !playlist.isEmpty()) {
                currentIndex = 0;
                loadTrack(0);
            }
        }
    }

    private void removeSelected() {
        int idx = playlistList.getSelectedIndex();
        if (idx < 0) return;
        playlist.remove(idx);
        playlistModel.remove(idx);
        if (currentIndex == idx) {
            stop();
            currentIndex = -1;
            titleLabel.setText("Нет трека");
            timeLabel.setText("00:00 / 00:00");
            progressBar.setValue(0);
        } else if (currentIndex > idx) {
            currentIndex--;
        }
    }

    private void clearPlaylist() {
        playlist.clear();
        playlistModel.clear();
        stop();
        currentIndex = -1;
        titleLabel.setText("Нет трека");
        timeLabel.setText("00:00 / 00:00");
        progressBar.setValue(0);
    }

    private void loadTrack(int index) {
        if (index < 0 || index >= playlist.size()) return;
        String filepath = playlist.get(index);
        titleLabel.setText(new File(filepath).getName());
        // JLayer не может получить длительность напрямую, используем приблизительную
        timeLabel.setText("00:00 / 00:00");
        progressBar.setValue(0);
    }

    private void playPause() {
        if (playlist.isEmpty()) {
            statusLabel.setText("Нет треков в плейлисте");
            return;
        }
        if (currentIndex == -1) {
            currentIndex = 0;
            loadTrack(0);
        }

        if (isPlaying && !isPaused) {
            // Пауза
            if (player != null) {
                // JLayer не поддерживает паузу, поэтому останавливаем и запоминаем позицию
                // Для простоты просто останавливаем
                stop();
                isPaused = true;
                playBtn.setText("▶");
                statusLabel.setText("Пауза");
            }
        } else if (isPaused) {
            // Продолжить — перезапускаем с начала (упрощённо)
            stop();
            playFromStart();
        } else {
            playFromStart();
        }
    }

    private void playFromStart() {
        if (currentIndex < 0 || currentIndex >= playlist.size()) return;
        stop();
        try {
            FileInputStream fis = new FileInputStream(playlist.get(currentIndex));
            player = new Player(fis);
            isPlaying = true;
            isPaused = false;
            playBtn.setText("⏸");
            statusLabel.setText("Играет");
            playThread = new Thread(() -> {
                try {
                    player.play();
                } catch (JavaLayerException e) {
                    e.printStackTrace();
                }
                // По окончании
                if (isPlaying) {
                    SwingUtilities.invokeLater(() -> {
                        isPlaying = false;
                        playBtn.setText("▶");
                        statusLabel.setText("Завершено");
                        nextTrack();
                    });
                }
            });
            playThread.start();
        } catch (Exception e) {
            statusLabel.setText("Ошибка: " + e.getMessage());
        }
    }

    private void stop() {
        if (player != null) {
            player.close();
            player = null;
        }
        if (playThread != null) {
            playThread.interrupt();
            playThread = null;
        }
        isPlaying = false;
        isPaused = false;
        playBtn.setText("▶");
        statusLabel.setText("Остановлено");
        progressBar.setValue(0);
    }

    private void nextTrack() {
        if (playlist.isEmpty()) return;
        currentIndex = (currentIndex + 1) % playlist.size();
        stop();
        loadTrack(currentIndex);
        playPause();
    }

    private void prevTrack() {
        if (playlist.isEmpty()) return;
        currentIndex = (currentIndex - 1 + playlist.size()) % playlist.size();
        stop();
        loadTrack(currentIndex);
        playPause();
    }

    private void updateProgress() {
        // JLayer не даёт позицию, поэтому просто показываем статус
        if (isPlaying && !isPaused) {
            progressBar.setValue((progressBar.getValue() + 1) % 100);
        }
    }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(AudioPlayer::new);
    }
}
