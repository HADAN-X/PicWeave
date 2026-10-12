using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PicWeave;

public sealed class ImageEntry(string path, BitmapSource thumbnail, int width, int height)
{
    public string Path { get; } = path;
    public string Name { get; } = System.IO.Path.GetFileName(path);
    public BitmapSource Thumbnail { get; } = thumbnail;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public string Dimensions => $"{Width} × {Height} px";
}

public partial class MainWindow : Window
{
    private const int MaxDimension = 30000;
    private const long MaxPixels = 80_000_000;
    private readonly ObservableCollection<ImageEntry> _images = [];
    private bool _loading;

    public MainWindow()
    {
        InitializeComponent();
        ImageList.ItemsSource = _images;
    }

    private async void AddImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要拼接的图片", Multiselect = true,
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|所有文件|*.*"
        };
        if (dialog.ShowDialog(this) == true) await AddFilesAsync(dialog.FileNames);
    }

    private async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var added = 0;
        var rejected = 0;
        _loading = true;
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_images.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) continue;
            try
            {
                var data = await Task.Run(() => LoadImage(path));
                _images.Add(new ImageEntry(path, data.Thumbnail, data.Width, data.Height));
                added++;
            }
            catch (Exception) { rejected++; }
        }
        _loading = false;
        if (_images.Count > 0 && ReferenceCombo.SelectedIndex < 0) ReferenceCombo.SelectedIndex = 0;
        RefreshStatus();
        if (rejected > 0) MessageBox.Show(this, $"有 {rejected} 个文件无法作为图片打开，已跳过。", "无法读取图片", MessageBoxButton.OK, MessageBoxImage.Information);
        if (added > 0) UpdateControls();
    }

    private static (int Width, int Height, BitmapSource Thumbnail) LoadImage(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        if (width < 1 || height < 1 || width > MaxDimension || height > MaxDimension || (long)width * height > MaxPixels)
            throw new InvalidDataException("Image exceeds supported dimensions.");
        var thumb = new TransformedBitmap(frame, new ScaleTransform(64d / Math.Max(width, height), 64d / Math.Max(width, height)));
        thumb.Freeze();
        return (width, height, thumb);
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) await AddFilesAsync(FilterImagePaths(paths));
    }

    private async void ImageList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) await AddFilesAsync(FilterImagePaths(paths));
    }

    private static IEnumerable<string> FilterImagePaths(IEnumerable<string> paths)
    {
        var allowed = new HashSet<string>([".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp"], StringComparer.OrdinalIgnoreCase);
        return paths.Where(x => File.Exists(x) && allowed.Contains(System.IO.Path.GetExtension(x)));
    }

    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

    private ImageEntry? EntryFor(object sender) => (sender as FrameworkElement)?.DataContext as ImageEntry;

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(EntryFor(sender), -1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(EntryFor(sender), 1);
    private void Move(ImageEntry? item, int delta)
    {
        if (item is null) return;
        var index = _images.IndexOf(item);
        var target = index + delta;
        if (target < 0 || target >= _images.Count) return;
        _images.Move(index, target);
        ImageList.SelectedItem = item;
        UpdateControls();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var item = EntryFor(sender);
        if (item is not null) _images.Remove(item);
        UpdateControls();
    }

    private void ClearImages_Click(object sender, RoutedEventArgs e)
    {
        _images.Clear();
        ReferenceCombo.SelectedIndex = -1;
        UpdateControls();
    }

    private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ImageList.SelectedItem is not ImageEntry selected) return;
        if (SizeCombo.SelectedIndex > 0) return;
        var idx = _images.IndexOf(selected);
        if (idx >= 0) ReferenceCombo.SelectedIndex = idx;
    }

    private void Settings_Changed(object sender, RoutedEventArgs e)
    {
        if (SizeCombo is null || SizePickerPanel is null || CustomSizePanel is null) return;
        bool custom = SizeCombo.SelectedIndex == 1;
        SizePickerPanel.Visibility = custom ? Visibility.Collapsed : Visibility.Visible;
        CustomSizePanel.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        ImageSizeInfo.Visibility = custom ? Visibility.Collapsed : Visibility.Visible;
        SizeDescription.Text = custom ? "输入每张图片对齐后的画布宽度和高度" : "选择一张输入图片作为每张图片的尺寸基准";
        UpdateControls();
    }

    private void UpdateControls()
    {
        if (ImageCountText is null) return;
        _loading = true;
        var previous = ReferenceCombo.SelectedItem as ImageEntry;
        ReferenceCombo.ItemsSource = _images;
        ReferenceCombo.SelectedItem = previous is not null && _images.Contains(previous) ? previous : (_images.Count > 0 ? _images[0] : null);
        _loading = false;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        ImageCountText.Text = _images.Count == 0 ? "添加图片开始拼接" : $"已添加 {_images.Count} 张图片 · 可调整拼接顺序";
        if (CurrentSizeText is null) return;
        if (SizeCombo.SelectedIndex == 1)
        {
            CurrentSizeText.Text = "自定义尺寸";
            return;
        }
        if (ReferenceCombo.SelectedItem is ImageEntry entry)
        {
            CurrentSizeText.Text = $"{entry.Width} × {entry.Height} 像素";
            SizeDescription.Text = $"{entry.Name} · 作为每张图片的统一尺寸";
        }
        else
        {
            CurrentSizeText.Text = "等待添加图片";
            SizeDescription.Text = "添加图片后即可选择尺寸基准";
        }
    }

    private bool TryGetSize(out int width, out int height, out long totalPixels)
    {
        width = height = 0;
        totalPixels = 0;
        if (_images.Count < 1)
        {
            MessageBox.Show(this, "请先添加至少一张图片。", "尚未添加图片", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
        if (SizeCombo.SelectedIndex == 1)
        {
            if (!int.TryParse(WidthBox.Text, out width) || !int.TryParse(HeightBox.Text, out height) || width < 1 || height < 1)
            {
                MessageBox.Show(this, "请输入有效的自定义宽度和高度。", "尺寸无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else if (ReferenceCombo.SelectedItem is ImageEntry reference)
        {
            width = reference.Width;
            height = reference.Height;
        }
        else return false;

        int axisCount = _images.Count;
        var vertical = DirectionCombo.SelectedIndex == 0;
        long fullWidth = vertical ? width : (long)width * axisCount;
        long fullHeight = vertical ? (long)height * axisCount : height;
        totalPixels = fullWidth * fullHeight;
        if (width > MaxDimension || height > MaxDimension || fullWidth > 50000 || fullHeight > 50000 || totalPixels > MaxPixels)
        {
            MessageBox.Show(this, $"拼接图尺寸过大（{fullWidth:N0} × {fullHeight:N0}）。请减少图片数量或选择更小的对齐尺寸。", "输出尺寸过大", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private static BitmapSource OpenBitmap(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
        bitmap.Freeze();
        return bitmap;
    }

    private Task<BitmapSource> BuildCompositionAsync(int width, int height, bool preview = false)
    {
        var completion = new TaskCompletionSource<BitmapSource>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try { completion.TrySetResult(BuildComposition(width, height, preview)); }
            catch (Exception ex) { completion.TrySetException(ex); }
        }) { IsBackground = true, Name = "PicWeave image renderer" };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
        return completion.Task;
    }

    private BitmapSource BuildComposition(int width, int height, bool preview)
    {
        var vertical = DirectionCombo.SelectedIndex == 0;
        int canvasWidth = vertical ? width : width * _images.Count;
        int canvasHeight = vertical ? height * _images.Count : height;
        double scale = preview ? Math.Min(1, Math.Min(1800d / canvasWidth, 1600d / canvasHeight)) : 1;
        int renderWidth = Math.Max(1, (int)Math.Round(canvasWidth * scale));
        int renderHeight = Math.Max(1, (int)Math.Round(canvasHeight * scale));
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, canvasWidth, canvasHeight));
            for (int i = 0; i < _images.Count; i++)
            {
                var bitmap = OpenBitmap(_images[i].Path);
                var cell = new Rect(vertical ? 0 : (double)i * width, vertical ? (double)i * height : 0, width, height);
                var mode = FitCombo.SelectedIndex;
                if (mode == 0)
                {
                    double factor = Math.Min((double)width / bitmap.PixelWidth, (double)height / bitmap.PixelHeight);
                    double w = bitmap.PixelWidth * factor;
                    double h = bitmap.PixelHeight * factor;
                    dc.DrawImage(bitmap, new Rect(cell.X + (width - w) / 2, cell.Y + (height - h) / 2, w, h));
                }
                else if (mode == 1)
                {
                    double factor = Math.Max((double)width / bitmap.PixelWidth, (double)height / bitmap.PixelHeight);
                    double w = bitmap.PixelWidth * factor;
                    double h = bitmap.PixelHeight * factor;
                    dc.PushClip(new RectangleGeometry(cell));
                    dc.DrawImage(bitmap, new Rect(cell.X + (width - w) / 2, cell.Y + (height - h) / 2, w, h));
                    dc.Pop();
                }
                else dc.DrawImage(bitmap, cell);
            }
        }
        var result = new RenderTargetBitmap(renderWidth, renderHeight, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSize(out int width, out int height, out _)) return;
        try
        {
            SetBusy(true);
            var image = await BuildCompositionAsync(width, height, preview: true);
            PreviewImage.Source = image;
            long outWidth = DirectionCombo.SelectedIndex == 0 ? width : (long)width * _images.Count;
            long outHeight = DirectionCombo.SelectedIndex == 0 ? (long)height * _images.Count : height;
            PreviewDimensions.Text = $"{outWidth:N0} × {outHeight:N0} px   ·   {_images.Count} 张图片";
            PreviewOverlay.Visibility = Visibility.Visible;
        }
        catch (Exception ex) { MessageBox.Show(this, $"预览生成失败：{ex.Message}", "出错了", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { SetBusy(false); }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSize(out int width, out int height, out _)) return;
        bool jpeg = FormatCombo.SelectedIndex == 1;
        var dialog = new SaveFileDialog
        {
            Title = "保存拼接图片", FileName = $"PicWeave_{DateTime.Now:yyyyMMdd_HHmmss}.{(jpeg ? "jpg" : "png")}",
            Filter = jpeg ? "JPEG 图片|*.jpg;*.jpeg" : "PNG 图片|*.png", AddExtension = true, DefaultExt = jpeg ? ".jpg" : ".png"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            SetBusy(true);
            var image = await BuildCompositionAsync(width, height);
            BitmapEncoder encoder;
            if (jpeg) encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            else encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            await using (var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                await Task.Run(() => encoder.Save(stream));
            PreviewOverlay.Visibility = Visibility.Collapsed;
            var open = MessageBox.Show(this, $"拼接图片已保存：\n{dialog.FileName}\n\n是否打开保存位置？", "拼接完成", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (open == MessageBoxResult.Yes) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{dialog.FileName}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, $"图片保存失败：{ex.Message}", "出错了", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        SaveButton.IsEnabled = !busy;
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }

    private void ClosePreview_Click(object sender, RoutedEventArgs e) => PreviewOverlay.Visibility = Visibility.Collapsed;
    private void PreviewOverlay_MouseDown(object sender, MouseButtonEventArgs e) { if (ReferenceEquals(e.OriginalSource, PreviewOverlay)) PreviewOverlay.Visibility = Visibility.Collapsed; }
    private void PreviewCard_MouseDown(object sender, MouseButtonEventArgs e) => e.Handled = true;
}
