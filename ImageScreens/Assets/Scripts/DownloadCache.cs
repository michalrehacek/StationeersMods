using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ImageScreens
{
    // Cache for data downloaded from an HTTP server.
    public class DownloadCache
    {
        // Listener for download events.
        public interface IListener
        {
            // Callback when the texture download finishes (success or fail).
            // The result parameter is never null.
            public void DownloadFinished(Result result);
        }

        // In-progress download.
        private class Download
        {
            // URL we're downloading.
            public string URL;
            // Is the URL a text file (slideshow), as opposed to an imahe?
            public bool IsText;

            // List of listeners waiting for this download.
            private readonly List<IListener> listeners = new();
            // The coroutine performing the download.
            public Coroutine routine;

            // Make a callback to all listeners waiting for this download.
            private void Callback(Result result)
            {
                foreach (var listener in listeners)
                {
                    listener.DownloadFinished(result);
                }
            }

            // Make a successful callback to all listeners waiting for this download.
            public void SuccessCallback(Entry entry)
            {
                Callback(Result.ofSuccess(entry));
            }

            // Make a failed callback to all listeners waiting for this download.
            public void FailCallback()
            {
                Callback(Result.ofFail(URL));
            }

            // Add the specified listener to the list of listeners, if it's not already present.
            public void AddListener(IListener listener)
            {
                if ( ! listeners.Contains(listener) )
                {
                    listeners.Add(listener);
                }
            }

            // Remove the specified listener from the list of listeners.
            public void RemoveListener(IListener listener)
            {
                if ( listeners.Remove(listener) )
                {
                    Log.Info($"Removed a screen from download listeners for {URL} because the screen has been destroyed");
                }
            }
        }

        // Cache entry.
        public class Entry
        {
            // URL from which the object is downloaded.
            public string URL;
            // Does the file represent a text (index) file, as opposed to a texture?
            public bool IsText;

            // The created texture.
            public Texture2D Texture;
            // The cached slideshow entries.
            public string[] Index;
        }

        // Result of a GetTexture operation.
        public class Result
        {
            // Did the download succeed?
            public bool Success = false;
            // The URL we were downloading.
            public string URL;
            // The cached entry.
            public Entry Data;

            // Create a successful result.
            public static Result ofSuccess(Entry entry)
            {
                return new Result
                {
                    Success = true,
                    URL = entry.URL,
                    Data = entry,
                };
            }

            // Create a failed result.
            public static Result ofFail(string URL)
            {
                return new Result
                {
                    Success = false,
                    URL = URL,
                };
            }
        }

        // The owner of the cache.
        // It's providing support for co-routines.
        private readonly MonoBehaviour owner;

        // The contents of the cache.
        // Keyed by URL.
        private readonly Dictionary<string, Entry> cache = new();

        // List of pending downloads.
        // Keyed by URL.
        private readonly Dictionary<string, Download> pending = new();

        // Constructor.
        public DownloadCache(MonoBehaviour owner)
        {
            this.owner = owner;
        }

        // Notification called when a listener is destroyed.
        public void ListenerDestroyed(IListener l)
        {
            // Remove the listener from all pending downloads.
            foreach ( var progress in pending.Values )
            {
                progress.RemoveListener(l);
            }
        }

        // Get a texture from the cache, or initiate download.
        // In any case, on success or failure, the callback will be invoked.
        public void DownloadFile(IListener listener, string url)
        {
            // If the texture is already cached, return it.
            if (cache.TryGetValue(url, out var cached))
            {
                Log.Info($"Cache: cache hit for \"{url}\"");
                listener.DownloadFinished(Result.ofSuccess(cached));
                return;
            }

            // Check pending downloads.
            if (pending.TryGetValue(url,out var progress))
            {
                // A download of the same URL is already in progress.
                // Add the listener to the list of those to be notified when the download finishes.
                Log.Info($"Cache: Download of \"{url}\" already in progress");
                progress.AddListener(listener);
                return;
            }

            // Otherwise initiate a download.
            var download = new Download()
            {
                URL = url,
                IsText = Utils.IsTextURL(url),
            };
            download.AddListener(listener);
            download.routine = owner.StartCoroutine(DownloadWorker(download));
            pending.Add(url, download);
        }

        // Download a texture from the specified URL and make the callback when done (success or fail).
        private IEnumerator DownloadWorker(Download download)
        {
            Log.Info($"Cache: Starting to download \"{download.URL}\"");

            // Initiate the HTTP request.
            using var request = download.IsText ? UnityWebRequest.Get(download.URL) : UnityWebRequestTexture.GetTexture(download.URL);
            yield return request.SendWebRequest();
            pending.Remove(download.URL);

            // If the request failed, make a failed callback.
            if (request.result != UnityWebRequest.Result.Success)
            {
                Log.Error($"Cache: Failed to download \"{download.URL}\": {request.error}");
                download.FailCallback();
                yield break;
            }
            Log.Info($"Cache: Successfully downloaded \"{download.URL}\"");

            // Process the successful download.
            if (download.IsText)
            {
                SuccessIndex(request, download);
            }
            else
            {
                SuccessImage(request, download);
            }
        }

        // Process a successful image download.
        private void SuccessImage(UnityWebRequest request,Download download)
        {
            // Create the texture.
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            texture.anisoLevel = 16;
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply(true);

            // Add the texture to the cache.
            var entry = new Entry
            {
                URL = download.URL,
                IsText = false,
                Texture = texture,
            };
            cache[download.URL] = entry;

            // Make a successful callback.
            download.SuccessCallback(entry);
        }

        // Process a successful download of an index file.
        private void SuccessIndex(UnityWebRequest request, Download download)
        {
            // If the content type isn't text, don't parse the response.
            string contentType = request.GetResponseHeader("Content-Type");
            if (! contentType.StartsWith("text/"))
            {
                Log.Info($"Cache: Slideshow index download error: Content-Type of \"{download.URL}\" response is \"{contentType}\"");
                download.FailCallback();
                return;
            }

            // Retrieve the string response and split it into lines.
            string text = request.downloadHandler.text;
            string[] lines = text.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            // If the index is empty, there's no slideshow.
            if (lines.Length == 0)
            {
                Log.Info($"Cache: Slideshow index download error: index file \"{download.URL}\" is empty");
                download.FailCallback();
                return;
            }

            // Find the prefix of the URL, so that we can build an URL relative
            // to the path where the index file was from.
            string urlPrefix = Utils.GetURLPrefix(download.URL);

            // Process each line.
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = urlPrefix + lines[i].Trim();
            }

            // Add the texture to the cache.
            var entry = new Entry
            {
                URL = download.URL,
                IsText = true,
                Index = lines,
            };
            cache[download.URL] = entry;

            // Make a successful callback.
            Debug.Log($"Cache: Slideshow index download success, got {lines.Length} files");
            download.SuccessCallback(entry);
        }

        // Clear the cache and destroy all textures in it.
        public void Clear()
        {
            StopDownloads();
            ClearCache();
            Log.Info("Cache: Cache cleared.");
        }

        // Stop all pending downloads.
        private void StopDownloads()
        {
            foreach (var down in pending.Values)
            {
                if (down.routine != null)
                {
                    owner.StopCoroutine(down.routine);
                }
            }
            pending.Clear();
        }

        // Clear the actual download cache.
        private void ClearCache()
        {
            // Destroy all textures in the cache.
            foreach (var item in cache.Values)
            {
                if (item.Texture != null)
                {
                    ImageScreens.Destroy(item.Texture);
                }
            }

            // Clear the actual cache.
            cache.Clear();
        }
    }
}
