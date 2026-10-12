# PicWeave

PicWeave is a small Windows desktop app for joining multiple photos into one image. Everything is processed on your computer.

## Download

Open the [Releases page](https://github.com/HADAN-X/PicWeave/releases/latest) and download **PicWeave-Windows-x64.zip**. Unzip it and run `PicWeave.exe`. The single-file app includes its .NET runtime; .NET does not need to be installed. Windows x64 is supported.

## Use

1. Add photos with **＋ 添加图片**, or drag them into the window.
2. Arrange their order with ↑ and ↓.
3. Choose vertical or horizontal stitching, then set a reference photo or custom canvas dimensions.
4. Choose whether each photo is fitted inside the canvas, cropped to fill it, or stretched.
5. Pick PNG or JPEG, then select **拼接并保存**. Preview is optional.

Input formats: PNG, JPEG, BMP, GIF, TIFF, and WebP. PNG output is lossless. JPEG uses quality 95. Images are never uploaded.

Each output image is limited to 80 megapixels to keep memory use manageable. Large output sizes display their dimensions and ask you to reduce the image count or reference size.

## Build from source

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet publish PicWeave.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The GitHub Actions workflow builds a Windows x64 download. Run the workflow manually to produce a temporary artifact, or push a `v*` tag to publish a GitHub Release.
