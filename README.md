# PicWeave

## 中文

PicWeave 是一款 Windows 图片拼接工具。图片在本地处理，不会上传。

### 下载

前往 [GitHub Releases](https://github.com/HADAN-X/PicWeave/releases/latest)，下载并解压 `PicWeave-Windows-x64.zip`，双击运行 `PicWeave.exe`。程序已包含运行环境，无需另外安装 .NET。支持 Windows x64。

### 使用方法

1. 点击「＋ 添加图片」选择多张图片，或将图片拖入窗口。
2. 点击图片旁的 ↑、↓ 调整拼接顺序；点击 × 移除图片。
3. 选择「纵向拼接」或「横向拼接」。
4. 选择尺寸基准：使用某张输入图片的尺寸，或选择「自定义尺寸」并填写宽、高。所有图片都会按此尺寸生成拼接单元。
5. 选择图片填充方式：「完整显示」会留白补齐，「铺满画布」会裁切边缘，「拉伸铺满」会改变图片比例。
6. 选择 PNG 或 JPEG。可先点「预览拼接」，再点「完成并保存图片」；也可直接点「拼接并保存」，然后选择保存位置。

支持 PNG、JPEG、BMP、GIF、TIFF 和 WebP 输入。PNG 无损保存；JPEG 质量为 95。输出最多 8000 万像素。

## English

PicWeave is a Windows app for stitching images. All processing happens locally; images are never uploaded.

### Download

Visit [GitHub Releases](https://github.com/HADAN-X/PicWeave/releases/latest), download and unzip `PicWeave-Windows-x64.zip`, then run `PicWeave.exe`. The app includes its runtime; no .NET installation is required. Windows x64 is supported.

### How to use

1. Click **＋ 添加图片** to select multiple images, or drag images into the window.
2. Use ↑ and ↓ beside an image to change the order; click × to remove it.
3. Choose vertical or horizontal stitching.
4. Choose a size reference: use the dimensions of an input image, or select a custom size and enter the width and height. Each image is fitted into a cell of this size.
5. Choose a fit mode: **Fit** preserves the whole image with padding, **Fill** crops the edges, and **Stretch** changes the image proportions.
6. Choose PNG or JPEG. Click **预览拼接** to preview, then **完成并保存图片**; or click **拼接并保存** to save directly and choose a location.

Input formats: PNG, JPEG, BMP, GIF, TIFF, and WebP. PNG is lossless; JPEG quality is 95. Output is limited to 80 megapixels.
