using System;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;

namespace Screensaver;

/// <summary>
/// Главная форма приложения
/// </summary>
public partial class ScreensaverForm : Form
{
    private PointF[] snowflakePositions; // Позиция снежинок
    private float[] fallingSpeeds; // Скорость падения
    private int[] snowflakeSizes; // Размер снежинок
    private Random random; // Рандом
    private Image snowflakeImage; // Изображение снежинки
    private Image backgroundImage; // Фоновое изображение
    private Bitmap? backBuffer; // Буфер для отрисовки
    private Graphics? bufferGraphics; // Graphics объект буфера
    private Graphics? screenGraphics; // Graphics объект экрана

    private const int SnowflakeCount = 150; // Количество снежинок на экране
    private const float BaseSpeed = 0.5f; // Базовая скорость падения снежинки
    private const float MaxSpeedMultiplier = 3f; // Максимальный множитель скорости для крупных снежинок
    private const float SpeedFactor = 25f; // Коэффициент для расчёта скорости на основе размера снежинки

    /// <summary>
    /// Инициализирует новый экземпляр формы ScreensaverForm.
    /// </summary>
    public ScreensaverForm()
    {
        InitializeComponent();

        random = new Random();
        snowflakePositions = new PointF[SnowflakeCount];
        fallingSpeeds = new float[SnowflakeCount];
        snowflakeSizes = new int[SnowflakeCount];

        screenGraphics = this.CreateGraphics();
        CreateBackBuffer();
        LoadImages();
        InitializeSnowflakes();
        
        animationTimer.Interval = 16;
        animationTimer.Tick += AnimationTimer_Tick;
        animationTimer.Start();
    }

    /// <summary>
    /// Создаёт буфер для отрисовки при изменении размера формы
    /// </summary>
    private void CreateBackBuffer()
    {
        bufferGraphics?.Dispose();
        backBuffer?.Dispose();
        backBuffer = new Bitmap(Width, Height);
        bufferGraphics = Graphics.FromImage(backBuffer);
        bufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    }

    /// <summary>
    /// Загружает изображения снежинки и фона
    /// </summary>
    private void LoadImages()
    {
        try
        {
            snowflakeImage = Screensaver.Properties.Resources.snowflake;
            backgroundImage = Screensaver.Properties.Resources.background;
        }
        catch (Exception)
        {
            Application.Exit();
        }
    }

    /// <summary>
    /// Инициализирует все снежинки
    /// </summary>
    private void InitializeSnowflakes()
    {
        var minInitialYOffset = 20; // Минимальное начальное смещение по Y
        var maxInitialYOffset = 200; // Максимальное начальное смещение по Y
        var minSnowflakeSize = 5; // Минимальный размер снежинки
        var maxSnowflakeSize = 26; // Максимальный размер снежинки
        var largeSnowflake = 15f; // Снежинки крупнее получают множитель к скорости

        var horizontalInterval = Width / (float)SnowflakeCount;

        for (var snowflakeIndex = 0; snowflakeIndex < SnowflakeCount; snowflakeIndex++)
        {
            var xPosition = horizontalInterval * snowflakeIndex + random.Next(-10, 10);
            var yPosition = -random.Next(minInitialYOffset, maxInitialYOffset);

            snowflakePositions[snowflakeIndex] = new PointF(xPosition, yPosition);
            snowflakeSizes[snowflakeIndex] = random.Next(minSnowflakeSize, maxSnowflakeSize);

            if (snowflakeSizes[snowflakeIndex] > largeSnowflake)
            {
                var speedFactor = snowflakeSizes[snowflakeIndex] / SpeedFactor;
                fallingSpeeds[snowflakeIndex] = BaseSpeed + speedFactor * MaxSpeedMultiplier;
            }
            else
            {
                fallingSpeeds[snowflakeIndex] = BaseSpeed;
            }
        }
    }

    /// <summary>
    /// Обновляет позиции снежинок
    /// </summary>
    private void UpdateSnowflakes()
    {
        var maxHorizontalOffsetPerFrame = 0.5f; // Максимальное горизотальное смещение за кадр
        
        for (var snowflakeIndex = 0; snowflakeIndex < SnowflakeCount; snowflakeIndex++)
        {
            // Двигаем снежинку
            snowflakePositions[snowflakeIndex].Y += fallingSpeeds[snowflakeIndex];
            snowflakePositions[snowflakeIndex].X += (float)((random.NextDouble() - maxHorizontalOffsetPerFrame) * maxHorizontalOffsetPerFrame);

            // Проверяем, достигла ли снежинка нижней границы
            if (snowflakePositions[snowflakeIndex].Y > Height + snowflakeSizes[snowflakeIndex])
            {
                ResetSnowflake(snowflakeIndex);
            }

            // Обработка боковых границ
            if (snowflakePositions[snowflakeIndex].X < -snowflakeSizes[snowflakeIndex])
            {
                snowflakePositions[snowflakeIndex].X = Width + snowflakeSizes[snowflakeIndex];
            }

            if (snowflakePositions[snowflakeIndex].X > Width + snowflakeSizes[snowflakeIndex])
            {
                snowflakePositions[snowflakeIndex].X = -snowflakeSizes[snowflakeIndex];
            }
        }
    }

    /// <summary>
    /// Отрисовывает кадр
    /// </summary>
    private void RenderFrame()
    {
        if (bufferGraphics != null && backBuffer != null && screenGraphics != null)
        {
            bufferGraphics.Clear(Color.Black);
            
            if (backgroundImage != null)
            {
                bufferGraphics.DrawImage(backgroundImage, 0, 0, Width, Height);
            }

            if (snowflakeImage != null)
            {
                for (var snowflakeIndex = 0; snowflakeIndex < SnowflakeCount; snowflakeIndex++)
                {
                    var currentSnowflake = snowflakePositions[snowflakeIndex];
                    var size = snowflakeSizes[snowflakeIndex];

                    if (currentSnowflake.Y <= Height + size && currentSnowflake.Y >= -size)
                    {
                        bufferGraphics.DrawImage(snowflakeImage, currentSnowflake.X, currentSnowflake.Y, size, size);
                    }
                }
            }
            
            screenGraphics.DrawImageUnscaled(backBuffer, 0, 0);
        }
    }

    /// <summary>
    /// Сбрасывает снежинку в верхнюю часть экрана
    /// </summary>
    private void ResetSnowflake(int snowflakeIndex)
    {
        snowflakePositions[snowflakeIndex].X = random.Next(0, Width);
        snowflakePositions[snowflakeIndex].Y = -snowflakeSizes[snowflakeIndex];

        var speedFactor = snowflakeSizes[snowflakeIndex] / SpeedFactor;
        fallingSpeeds[snowflakeIndex] = BaseSpeed + speedFactor * MaxSpeedMultiplier;
    }

    /// <summary>
    /// Освобождает ресурсы
    /// </summary>
    private void CleanupResources()
    {
        animationTimer.Stop();
        animationTimer.Dispose();
        snowflakeImage.Dispose();
        backgroundImage?.Dispose();
        bufferGraphics?.Dispose();
        backBuffer?.Dispose();
        screenGraphics?.Dispose();
    }

    /// <summary>
    /// Обработчик таймера анимации.
    /// </summary>
    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        UpdateSnowflakes();  // Только обновляем позиции
        RenderFrame();       // Только отрисовываем
    }

    /// <summary>
    /// Обработчик нажатия клавиши
    /// </summary>
    private void ScreensaverForm_KeyDown(object sender, KeyEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Обработчик клика мыши
    /// </summary>
    private void ScreensaverForm_MouseClick(object sender, MouseEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Обработчик изменения размера формы
    /// </summary>
    private void ScreensaverForm_Resize(object sender, EventArgs e)
    {
        if (Width > 0 && Height > 0)
        {
            CreateBackBuffer();
        }
    }

    /// <summary>
    /// Обработчик закрытия формы
    /// </summary>
    private void ScreensaverForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        CleanupResources();
    }
}