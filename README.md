AudioForge — Аудиоплеер (MP3) на 7 языках
AudioForge — коллекция из семи независимых реализаций аудиоплеера для воспроизведения MP3-файлов. Каждая версия работает на своём языке программирования и предлагает базовый набор функций для прослушивания музыки: воспроизведение, пауза, остановка, регулировка громкости, отображение времени и управление плейлистом.

✨ Общие возможности
▶️ Воспроизведение MP3-файлов (локальных или по URL)

⏸️ Пауза и продолжение воспроизведения

⏹️ Остановка с возвратом в начало

🔊 Регулировка громкости

📊 Отображение текущего времени и общей длительности трека

📋 Плейлист — добавление, удаление, переключение треков

🎵 Поддержка метаданных (название, исполнитель, альбом) — в большинстве реализаций

🖥️ Интерфейсы:

Десктопные GUI: Python (Tkinter + pygame), Java (Swing + JLayer), C# (WinForms + NAudio)

Веб-приложения: JavaScript (HTML+CSS+Web Audio API), Go (Web + HTML5 Audio), Rust (Web + HTML5 Audio), PHP (Web + HTML5 Audio)

📋 Сравнение реализаций
Язык	Интерфейс	Библиотека для MP3	Плейлист	Громкость	Прогресс
Python	Tkinter GUI	pygame / mutagen	✅	✅	✅
JavaScript	Веб (HTML+CSS)	Web Audio API	✅	✅	✅
Go	Веб (сервер)	HTML5 Audio (клиент)	✅	✅	✅
Rust	Веб (сервер)	HTML5 Audio (клиент)	✅	✅	✅
Java	Swing GUI	JLayer (javazoom)	✅	✅	✅
C#	WinForms GUI	NAudio	✅	✅	✅
PHP	Веб (сервер)	HTML5 Audio (клиент)	✅	✅	✅
🚀 Быстрый старт
Python
bash
pip install pygame mutagen
python audio_player.py
JavaScript (браузер)
Откройте audio_player.html в браузере.

Go
bash
go run audio_player.go
# Откройте http://localhost:8080
Rust
bash
cargo run
# Откройте http://localhost:8000
Java
bash
# Скачайте JLayer: https://github.com/umjammer/jlayer
javac -cp ".:jl1.0.1.jar" AudioPlayer.java
java -cp ".:jl1.0.1.jar" AudioPlayer
C#
bash
# Установите NAudio: Install-Package NAudio
csc /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:NAudio.dll AudioPlayer.cs
AudioPlayer.exe
PHP
bash
php -S localhost:8000
# Откройте http://localhost:8000/audio_player.php
