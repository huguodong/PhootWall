using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Graphics;
using Java.Net;

namespace PhootWall
{
    public class PhotoWallAdapter : ArrayAdapter<String>, GridView.IOnScrollListener
    {
        public class ViewData : Java.Lang.Object
        {
            public ImageView img { get; set; }

        }
        /**
      * 记录所有正在下载或等待下载的任务。
      */
        private static ISet<BitmapWorkerTask> taskCollection;

        /**
         * 图片缓存技术的核心类，用于缓存所有下载好的图片，在程序内存达到设定值时会将最少最近使用的图片移除掉。
         */
        private static LruCache mMemoryCache;
        /**
         * GridView的实例
         */
        public static GridView mPhotoWall;

        /**
         * 第一张可见图片的下标
         */
        private int mFirstVisibleItem;

        /**
         * 一屏有多少张图片可见
         */
        private int mVisibleItemCount;

        /**
         * 记录是否刚打开程序，用于解决进入程序不滚动屏幕，不会下载图片的问题。
         */
        private bool isFirstEnter = true;

        public PhotoWallAdapter(Context context, int textViewResourceId, String[] objects, GridView photoWall)
            : base(context, textViewResourceId, objects)
        {
            mPhotoWall = photoWall;
            taskCollection = new HashSet<BitmapWorkerTask>();
            // 获取应用程序最大可用内存
            int maxMemory = (int)Java.Lang.Runtime.GetRuntime().MaxMemory();
            int cacheSize = maxMemory / 8;
            // 设置图片缓存大小为程序最大可用内存的1/8
            mMemoryCache = new LruCache(cacheSize);
            mPhotoWall.SetOnScrollListener(this);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            String url = GetItem(position);


            if (convertView == null)
            {
                convertView = LayoutInflater.From(Context).Inflate(Resource.Layout.PhoteLayout, null);
                ViewData vd = new ViewData();
                vd.img = (ImageView)convertView.FindViewById(Resource.Id.photo);

                convertView.Tag = vd;
                vd.img.Tag = url;
                SetImageView(url, vd.img);
            }
            else
            {
                ViewData vd = (ViewData)convertView.Tag;

            }

            return convertView;
        }

        private void SetImageView(String imageUrl, ImageView imageView)
        {

            Bitmap bitmap = GetBitmapFromMemoryCache(imageUrl);
            if (bitmap != null)
            {
                imageView.SetImageBitmap(bitmap);
            }
            else
            {
                imageView.SetImageResource(Resource.Drawable.empty_photo);
            }
        }

        public static void addBitmapToMemoryCache(String key, Bitmap bitmap)
        {
            if (GetBitmapFromMemoryCache(key) == null)
            {
                mMemoryCache.Put(key, bitmap);
            }
        }

        public static Bitmap GetBitmapFromMemoryCache(String key)
        {
            var a = mMemoryCache.Get(key);
            return (Bitmap)a;
        }





        public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            // 仅当GridView静止时才去下载图片，GridView滑动时取消所有正在下载的任务
            if (scrollState == ScrollState.Idle)
            {
                loadBitmaps(mFirstVisibleItem, mVisibleItemCount);
            }
            else
            {
                cancelAllTasks();
            }
        }

        public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
        {
            mFirstVisibleItem = firstVisibleItem;
            mVisibleItemCount = visibleItemCount;
            // 下载的任务应该由onScrollStateChanged里调用，但首次进入程序时onScrollStateChanged并不会调用，
            // 因此在这里为首次进入程序开启下载任务。
            if (isFirstEnter && visibleItemCount > 0)
            {
                loadBitmaps(firstVisibleItem, visibleItemCount);
                isFirstEnter = false;
            }
        }

        private void loadBitmaps(int firstVisibleItem, int visibleItemCount)
        {
            try
            {
                for (int i = firstVisibleItem; i < firstVisibleItem + visibleItemCount; i++)
                {
                    String imageUrl = Images.imageThumbUrls[i];
                    var bitmap = GetBitmapFromMemoryCache(imageUrl);
                    if (bitmap == null)
                    {
                        BitmapWorkerTask task = new BitmapWorkerTask();
                        taskCollection.Add(task);
                        task.Execute(imageUrl);
                    }
                    else
                    {
                        ImageView imageView = (ImageView)mPhotoWall.FindViewWithTag(imageUrl);
                        if (imageView != null && bitmap != null)
                        {
                            imageView.SetImageBitmap(bitmap);
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
        }

        public void cancelAllTasks()
        {
            if (taskCollection != null)
            {
                foreach (BitmapWorkerTask task in taskCollection)
                {
                    task.Cancel(false);
                }
            }
        }
        public class BitmapWorkerTask : AsyncTask<String, Java.Lang.Void, Bitmap>
        {

            /**
             * 图片的URL地址
             */
            private string imageUrl;
        
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] native_parms)
            {
                imageUrl = native_parms[0].ToString();
                // 在后台开始下载图片
                Bitmap bitmap = downloadBitmap(native_parms[0].ToString());
                if (bitmap != null)
                {
                    // 图片下载完成后缓存到LrcCache中
                    addBitmapToMemoryCache(native_parms[0].ToString(), bitmap);

                }
      
                return bitmap;
            }

            protected override void OnPostExecute(Bitmap bitmap)
            {
                base.OnPostExecute(bitmap);
                // 根据Tag找到相应的ImageView控件，将下载好的图片显示出来。
                ImageView imageView = (ImageView)mPhotoWall.FindViewWithTag(imageUrl);

                if (imageView != null && bitmap != null)
                {
                    imageView.SetImageBitmap(bitmap);
                }
                taskCollection.Remove(this);
            }

            /**
             * 建立HTTP请求，并获取Bitmap对象。
             * 
             * @param imageUrl
             *            图片的URL地址
             * @return 解析后的Bitmap对象
             */
            private Bitmap downloadBitmap(String imageUrl)
            {
                URL myFileUrl = null;
                Bitmap bitmap = null;
                try
                {

                    myFileUrl = new URL(imageUrl);
                    HttpURLConnection conn = (HttpURLConnection)myFileUrl.OpenConnection();
                    conn.DoInput = true;
                    conn.Connect();
                    System.IO.Stream iss = conn.InputStream;
                    bitmap = BitmapFactory.DecodeStream(iss);
                    iss.Close();

                }
                catch (Exception e)
                {

                }
                return bitmap;
            }
            protected override Bitmap RunInBackground(params string[] @params)
            {
                return  (Bitmap)DoInBackground(@params);
                
            }
        }
    }
  
}