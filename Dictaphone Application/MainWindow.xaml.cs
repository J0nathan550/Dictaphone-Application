using NAudio.Wave;
using System;
using System.IO;
using System.Timers;
using System.Windows;
using System.Text.Json;

namespace Dictaphone_Application
{
    public partial class MainWindow : Window
    {
        private Timer countTimer = new Timer() { Interval = 1000 };
        private TimeSpan timeCount = new TimeSpan();
        private bool isRecording = false;
        private WaveIn? sourceStream = new WaveIn();
        private WaveFileWriter? waveWriter;
        private string FilePath = string.Empty;
        private string FileName = string.Empty;
        private int InputDeviceIndex;
        private int fileIndex = 0;

        class Save
        {
            public int soundKhz { get; set; } = 44100;
        }

        private Save save = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadSaveFile();
            sliderSound.Minimum = 179;
            sliderSound.Value = (double)save.soundKhz;
            sliderSound.Maximum = 44100;
            countTimer.Elapsed += CountTimer_Elapsed;
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfigurationFile();
        }

        private void LoadSaveFile()
        {
            try
            {
                if (File.Exists(@"save.json"))
                {
                    string json = File.ReadAllText("save.json");
                    save = JsonSerializer.Deserialize<Save>(json);
                    if (save.soundKhz > 41000)
                    {
                        save.soundKhz = 41000;
                    }
                }
                else
                {
                    FileStream fs = File.Create("save.json");
                    fs.Flush();
                    fs.Close();
                    File.WriteAllText(@"save.json", JsonSerializer.Serialize(save));
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveConfigurationFile()
        {
            try
            {
                save.soundKhz = (int)sliderSound.Value;
                if (File.Exists(@"save.json"))
                {
                    File.WriteAllText(@"save.json", JsonSerializer.Serialize(save));
                }
                else
                {
                    FileStream fs = File.Create(@"save.json");
                    fs.Flush();
                    fs.Close();
                    File.WriteAllText(@"save.json", JsonSerializer.Serialize(save));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CountTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            timeCount = timeCount.Add(TimeSpan.FromSeconds(1));
            await Dispatcher.BeginInvoke(() =>
            {
                timeLabel.Content = $"{timeCount.Hours.ToString("00")}:{timeCount.Minutes.ToString("00")}:{timeCount.Seconds.ToString("00")}";
            });
        }

        private void StartRecording()
        {
            try
            {
                save.soundKhz = (int)sliderSound.Value;
                sourceStream = new WaveIn
                {
                    DeviceNumber = InputDeviceIndex,
                    WaveFormat = new WaveFormat(save.soundKhz, WaveIn.GetCapabilities(InputDeviceIndex).Channels)
                };

                sourceStream.DataAvailable += SourceStreamDataAvailable;

                FilePath = @"Audios\";

                if (!Directory.Exists(FilePath))
                {
                    Directory.CreateDirectory(FilePath);
                }

                FileName = $"{fileIndex}.wav";

                waveWriter = new WaveFileWriter(FilePath + FileName, sourceStream.WaveFormat);
                sourceStream.StartRecording();
                fileIndex++;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SourceStreamDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveWriter == null) return;
            waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            waveWriter.Flush();
        }

        private void RecordEnd()
        {
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                sourceStream.Dispose();
                sourceStream = null;
            }
            if (this.waveWriter == null)
            {
                return;
            }
            this.waveWriter.Dispose();
            this.waveWriter = null;
        }
        private void Record_Click(object sender, RoutedEventArgs e)
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                recordButton.Content = "Остановить";
                countTimer.Start();
                timeLabel.Content = "00:00:00";
                timeCount = TimeSpan.Parse("00:00:00");
                StartRecording();
            }
            else
            {
                recordButton.Content = "Запись";
                timeCount = TimeSpan.Parse("00:00:00");
                timeLabel.Content = "00:00:00";
                countTimer.Stop();
                RecordEnd();
            }
        }

        private void QualityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int value = (int)sliderSound.Value;
            khzSoundLabel.Content = value.ToString();
        }

        private void GiveInformation(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Приветствую в самом простом диктафоне.\n\nВы можете настроить качество звука используя полоску ниже.\n\nФайлы сохраняются там где находится програма в папке \"Audios\", звуки нумерются по индексу.\n\nПосле перезапуска индексация сбросится до нуля и оно может перезаписывать звуки которые были ранее записаны.\n\nПриятного использования, програму создал J0nathan550.\n\nVersion: 1.0, 22.07.2023", "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}