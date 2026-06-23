#!/usr/bin/env python3
# audio_player.py - Аудиоплеер на Python (Tkinter + pygame)
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import pygame
import os
import threading
import time
from mutagen.mp3 import MP3
from mutagen import File
import glob

class AudioPlayer:
    def __init__(self, root):
        self.root = root
        self.root.title("🎵 AudioForge - Python")
        self.root.geometry("600x500")
        self.root.resizable(False, False)

        # Инициализация pygame mixer
        pygame.mixer.init()
        self.current_file = None
        self.is_playing = False
        self.is_paused = False
        self.position = 0
        self.total_length = 0
        self.playlist = []
        self.current_index = -1

        self.create_widgets()
        self.update_thread()

    def create_widgets(self):
        # Верхняя панель
        top = tk.Frame(self.root, bg="#2c3e50", height=60)
        top.pack(fill=tk.X)
        tk.Label(top, text="🎵 AudioForge", font=('Arial', 18, 'bold'),
                 fg="white", bg="#2c3e50").pack(side=tk.LEFT, padx=20, pady=15)

        # Кнопки управления
        control_frame = tk.Frame(self.root, bg="#ecf0f1", height=70)
        control_frame.pack(fill=tk.X)

        btn_style = {'bg': '#3498db', 'fg': 'white', 'font': ('Arial', 12), 'width': 6}
        self.play_btn = tk.Button(control_frame, text="▶", command=self.play_pause, **btn_style)
        self.play_btn.pack(side=tk.LEFT, padx=5, pady=10)

        self.stop_btn = tk.Button(control_frame, text="⏹", command=self.stop, **btn_style)
        self.stop_btn.pack(side=tk.LEFT, padx=5, pady=10)

        tk.Button(control_frame, text="⏭", command=self.next_track, **btn_style).pack(side=tk.LEFT, padx=5, pady=10)
        tk.Button(control_frame, text="⏮", command=self.prev_track, **btn_style).pack(side=tk.LEFT, padx=5, pady=10)

        # Громкость
        tk.Label(control_frame, text="🔊", font=('Arial', 14), bg="#ecf0f1").pack(side=tk.LEFT, padx=(20,5))
        self.volume_var = tk.IntVar(value=80)
        volume_scale = tk.Scale(control_frame, from_=0, to=100, orient=tk.HORIZONTAL,
                                variable=self.volume_var, length=100, command=self.set_volume)
        volume_scale.pack(side=tk.LEFT, padx=5)

        # Информация о треке
        info_frame = tk.Frame(self.root, bg="#f8f9fa")
        info_frame.pack(fill=tk.X, pady=5)
        self.title_label = tk.Label(info_frame, text="Нет трека", font=('Arial', 12, 'bold'), bg="#f8f9fa")
        self.title_label.pack(side=tk.LEFT, padx=20)
        self.time_label = tk.Label(info_frame, text="00:00 / 00:00", font=('Arial', 10), bg="#f8f9fa")
        self.time_label.pack(side=tk.RIGHT, padx=20)

        # Прогресс-бар
        self.progress = ttk.Progressbar(self.root, orient=tk.HORIZONTAL, length=560, mode='determinate')
        self.progress.pack(pady=5)

        # Плейлист
        playlist_frame = tk.Frame(self.root)
        playlist_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        tk.Label(playlist_frame, text="📋 Плейлист", font=('Arial', 12, 'bold')).pack(anchor=tk.W)

        list_frame = tk.Frame(playlist_frame)
        list_frame.pack(fill=tk.BOTH, expand=True)
        self.playlist_box = tk.Listbox(list_frame, font=('Arial', 10), selectmode=tk.SINGLE)
        self.playlist_box.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar = tk.Scrollbar(list_frame)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.playlist_box.config(yscrollcommand=scrollbar.set)
        scrollbar.config(command=self.playlist_box.yview)

        # Кнопки управления плейлистом
        playlist_controls = tk.Frame(playlist_frame)
        playlist_controls.pack(fill=tk.X, pady=5)
        tk.Button(playlist_controls, text="➕ Добавить", command=self.add_files,
                  bg="#2ecc71", fg="white").pack(side=tk.LEFT, padx=5)
        tk.Button(playlist_controls, text="🗑 Удалить", command=self.remove_selected,
                  bg="#e74c3c", fg="white").pack(side=tk.LEFT, padx=5)
        tk.Button(playlist_controls, text="🔄 Очистить", command=self.clear_playlist,
                  bg="#95a5a6", fg="white").pack(side=tk.LEFT, padx=5)

        self.playlist_box.bind("<Double-Button-1>", lambda e: self.play_selected())

        # Статус
        self.status_label = tk.Label(self.root, text="Готов", font=('Arial', 9), fg="#7f8c8d")
        self.status_label.pack(pady=2)

        # Пример плейлиста
        self.add_sample_files()

    def add_sample_files(self):
        # Добавляем примеры, если есть MP3 в папке
        files = glob.glob("*.mp3")
        for f in files[:5]:
            self.playlist.append(f)
            self.playlist_box.insert(tk.END, os.path.basename(f))
        if self.playlist:
            self.current_index = 0
            self.load_track(self.playlist[0])

    def add_files(self):
        files = filedialog.askopenfilenames(filetypes=[("MP3 files", "*.mp3"), ("All files", "*.*")])
        for f in files:
            self.playlist.append(f)
            self.playlist_box.insert(tk.END, os.path.basename(f))
        if not self.current_file and self.playlist:
            self.current_index = 0
            self.load_track(self.playlist[0])

    def remove_selected(self):
        selection = self.playlist_box.curselection()
        if not selection:
            return
        idx = selection[0]
        self.playlist.pop(idx)
        self.playlist_box.delete(idx)
        if self.current_index == idx:
            self.stop()
            self.current_index = -1
            self.current_file = None
            self.title_label.config(text="Нет трека")
            self.time_label.config(text="00:00 / 00:00")
            self.progress['value'] = 0
        elif self.current_index > idx:
            self.current_index -= 1

    def clear_playlist(self):
        self.playlist.clear()
        self.playlist_box.delete(0, tk.END)
        self.stop()
        self.current_index = -1
        self.current_file = None
        self.title_label.config(text="Нет трека")
        self.time_label.config(text="00:00 / 00:00")
        self.progress['value'] = 0

    def load_track(self, filepath):
        self.current_file = filepath
        try:
            audio = MP3(filepath)
            self.total_length = audio.info.length
            title = os.path.basename(filepath)
            # Пытаемся получить метаданные
            try:
                if audio.get('TIT2'):
                    title = str(audio['TIT2'])
            except:
                pass
            self.title_label.config(text=title)
            self.time_label.config(text=f"00:00 / {self.format_time(self.total_length)}")
            self.progress['value'] = 0
        except:
            self.total_length = 0
            self.title_label.config(text=os.path.basename(filepath))
            self.time_label.config(text="00:00 / 00:00")

    def play_pause(self):
        if not self.current_file:
            if self.playlist:
                self.current_index = 0
                self.load_track(self.playlist[0])
            else:
                messagebox.showwarning("Нет треков", "Добавьте файлы в плейлист")
                return

        if self.is_playing and not self.is_paused:
            # Пауза
            pygame.mixer.music.pause()
            self.is_paused = True
            self.play_btn.config(text="▶")
            self.status_label.config(text="Пауза")
        elif self.is_paused:
            # Продолжить
            pygame.mixer.music.unpause()
            self.is_paused = False
            self.play_btn.config(text="⏸")
            self.status_label.config(text="Играет")
        else:
            # Начать
            self.play_btn.config(text="⏸")
            self.is_playing = True
            self.is_paused = False
            pygame.mixer.music.load(self.current_file)
            pygame.mixer.music.play()
            self.status_label.config(text="Играет")
            # Устанавливаем громкость
            self.set_volume(self.volume_var.get())

    def stop(self):
        pygame.mixer.music.stop()
        self.is_playing = False
        self.is_paused = False
        self.play_btn.config(text="▶")
        self.status_label.config(text="Остановлено")
        self.position = 0
        self.progress['value'] = 0
        self.time_label.config(text=f"00:00 / {self.format_time(self.total_length)}")

    def set_volume(self, val):
        volume = int(val) / 100.0
        pygame.mixer.music.set_volume(volume)

    def next_track(self):
        if not self.playlist:
            return
        self.current_index = (self.current_index + 1) % len(self.playlist)
        self.stop()
        self.load_track(self.playlist[self.current_index])
        self.play_pause()

    def prev_track(self):
        if not self.playlist:
            return
        self.current_index = (self.current_index - 1) % len(self.playlist)
        self.stop()
        self.load_track(self.playlist[self.current_index])
        self.play_pause()

    def play_selected(self):
        selection = self.playlist_box.curselection()
        if not selection:
            return
        self.current_index = selection[0]
        self.stop()
        self.load_track(self.playlist[self.current_index])
        self.play_pause()

    def update_thread(self):
        if self.is_playing and not self.is_paused:
            try:
                pos = pygame.mixer.music.get_pos() / 1000.0
                if pos > 0:
                    self.position = pos
                    if self.total_length > 0:
                        progress = (pos / self.total_length) * 100
                        self.progress['value'] = progress
                        self.time_label.config(
                            text=f"{self.format_time(pos)} / {self.format_time(self.total_length)}"
                        )
            except:
                pass
            # Проверка окончания трека
            if self.is_playing and not pygame.mixer.music.get_busy() and not self.is_paused:
                self.is_playing = False
                self.play_btn.config(text="▶")
                self.status_label.config(text="Завершено")
                self.progress['value'] = 0
                self.time_label.config(text=f"00:00 / {self.format_time(self.total_length)}")
                # Автоматически переключить на следующий трек
                if self.playlist:
                    self.next_track()

        self.root.after(100, self.update_thread)

    def format_time(self, seconds):
        m = int(seconds // 60)
        s = int(seconds % 60)
        return f"{m:02d}:{s:02d}"

if __name__ == "__main__":
    root = tk.Tk()
    app = AudioPlayer(root)
    root.mainloop()
