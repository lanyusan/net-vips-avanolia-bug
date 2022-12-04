using Avalonia.Controls;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing.Drawing2D;
using NetVips;
using System;
using System.Collections.Generic;
using Avalonia.Animation;

namespace NetVipsAvalonia {
public partial class MainWindow : Window {
  public MainWindow() {
    InitializeComponent();
    GenBtn.Click += async (s, e) => {
      var openFolderWindow = new OpenFolderDialog();
      var folder = await openFolderWindow.ShowAsync(this);

      if (folder is null) {
        return;
      }
      StateLabel.Content = "Started...";

      var imageFiles = Directory.GetFiles(folder);

      var pngs = imageFiles.Where(filename => filename.EndsWith("png"))
                     .Select(filename => NetVips.Image.NewFromFile(filename))
                     .ToArray();
      var framesCount = 0;
      var gifFiles = imageFiles.Where(filename => filename.EndsWith("gif"));

      List<NetVips.Image[]> gifsList = new();

      foreach (var filename in gifFiles) {
        var gif = NetVips.Image.Gifload(filename, -1);
        Debug.WriteLine("Gif bands ==> " + gif.Bands);
        var pages = (int)(gif.Height / gif.PageHeight);
        var frames = new List<NetVips.Image>();
        for (var i = 0; i < pages; i++) {
          var frame =
              gif.Crop(0, i * gif.PageHeight, gif.Width, gif.PageHeight);
          frames.Add(frame);
        }
        if (framesCount == 0) {
          framesCount = frames.Count;
        } else {
          framesCount = Math.Min(framesCount, frames.Count);
        }
        gifsList.Add(frames.ToArray());
      }
      var gifs = gifsList.ToArray();

      DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;

      var compositModes =
          Enum.GetValues(typeof(Enums.BlendMode)).Cast<Enums.BlendMode>();

      // var compositModes = new List<Enums.BlendMode>() { Enums.BlendMode.Over
      // };

      foreach (var mode in compositModes) {

        var delays = new List<int>();
        var generatedFramesList = new List<NetVips.Image>();

        for (var i = 0; i < framesCount; i++) {
          delays.Add(100);
          var baseImage = pngs[0];
          var forLayering = new List<NetVips.Image>();
          var blendModes = new List<Enums.BlendMode>();
          for (var j = 1; j < pngs.Length; j++) {
            forLayering.Add(pngs[j]);
            blendModes.Add(Enums.BlendMode.Over);
          }

          foreach (var gif in gifs) {
            forLayering.Add(gif[i]);
            blendModes.Add(mode);
          }

          var blendedFrame =
              baseImage.Composite(forLayering.ToArray(), blendModes.ToArray());
          generatedFramesList.Add(blendedFrame);
        }

        var generatedFrames = generatedFramesList.ToArray();

        var framesJoined = NetVips.Image.Arrayjoin(generatedFrames, 1);

        var outputFileName = now.ToUnixTimeMilliseconds() + "-" + mode + ".gif";

        var filepath = Path.Join(folder, "out", outputFileName);

        var filepathJoined = filepath + "-joined.png";
        framesJoined.Pngsave(filepathJoined);
        var height = generatedFrames[0].Height;
        var framesWithMeta = framesJoined.Mutate(mutable => {
          // Set the number of loops, libvips uses iterations like this:
          // 0 - set 0 loops (infinite)
          // 1 - loop once
          // 2 - loop twice etc.
          mutable.Set(GValue.GIntType, "loop", 0);

          // Set the frame delay(s)
          // We use a 1000ms delay for all frames in this example
          mutable.Set(GValue.ArrayIntType, "delay", delays.ToArray());

          // Set page height
          mutable.Set(GValue.GIntType, "page-height", height);
        });
        framesWithMeta.Gifsave(filepath, dither: 0.5, effort: 1,
                               bitdepth: 8, reoptimise: true);
      }

      StateLabel.Content = "Wrote to folder: " + Path.Join(folder, "out");
    };
  }
}
}
