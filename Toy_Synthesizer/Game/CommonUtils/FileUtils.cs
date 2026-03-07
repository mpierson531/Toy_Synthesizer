using System;
using System.IO;

namespace Toy_Synthesizer.Game.CommonUtils
{
    public static class FileUtils
    {
        // Returns true if data was written to the file.
        // onFail will be invoked if it is not null and an exception is caught.
        public static bool Write(string folder, string path, byte[] data,
                                 Func<bool> onDirectoryNotFound = null,
                                 Func<bool> onFileNotFound = null,
                                 Action<Exception> onFail = null)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    if (onDirectoryNotFound is not null && !onDirectoryNotFound())
                    {
                        return false;
                    }

                    Directory.CreateDirectory(folder);
                }

                if (!File.Exists(path) && onFileNotFound is not null && !onFileNotFound())
                {
                    return false;
                }

                File.WriteAllBytes(path, data);

                return true;
            }
            catch (Exception e)
            {
                onFail?.Invoke(e);

                return false;
            }
        }

        // Returns true if data was written to the file.
        // onFail will be invoked if it is not null and an exception is caught.
        public static bool Read(string path,
                                out byte[] data,
                                Action onFileNotFound = null,
                                Action<Exception> onFail = null)
        {
            data = null;

            try
            {
                if (!File.Exists(path))
                {
                    onFileNotFound?.Invoke();

                    return false;
                }

                data = File.ReadAllBytes(path);

                return true;
            }
            catch (Exception e)
            {
                onFail?.Invoke(e);

                return false;
            }
        }
    }
}
