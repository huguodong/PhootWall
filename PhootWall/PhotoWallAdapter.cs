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
      * ��¼�����������ػ�ȴ����ص�����
      */
        private static ISet<BitmapWorkerTask> taskCollection;

        /**
         * ͼƬ���漼���ĺ����࣬���ڻ����������غõ�ͼƬ���ڳ����ڴ�ﵽ�趨ֵʱ�Ὣ�������ʹ�õ�ͼƬ�Ƴ�����
         */
        private static LruCache mMemoryCache;
        /**
         * GridView��ʵ��
         */
        public static GridView mPhotoWall;

        /**
         * ��һ�ſɼ�ͼƬ���±�
         */
        private int mFirstVisibleItem;

        /**
         * һ���ж�����ͼƬ�ɼ�
         */
        private int mVisibleItemCount;

        /**
         * ��¼�Ƿ�մ򿪳������ڽ��������򲻹�����Ļ����������ͼƬ�����⡣
         */
        private bool isFirstEnter = true;

        public PhotoWallAdapter(Context context, int textViewResourceId, String[] objects, GridView photoWall)
            : base(context, textViewResourceId, objects)
        {
            mPhotoWall = photoWall;
            taskCollection = new HashSet<BitmapWorkerTask>();
            // ��ȡӦ�ó����������ڴ�
            int maxMemory = (int)Java.Lang.Runtime.GetRuntime().MaxMemory();
            int cacheSize = maxMemory / 8;
            // ����ͼƬ�����СΪ�����������ڴ��1/8
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
            // ����GridView��ֹʱ��ȥ����ͼƬ��GridView����ʱȡ�������������ص�����
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
            // ���ص�����Ӧ����onScrollStateChanged����ã����״ν������ʱonScrollStateChanged��������ã�
            // ���������Ϊ�״ν����������������
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
             * ͼƬ��URL��ַ
             */
            private string imageUrl;
        
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] native_parms)
            {
                imageUrl = native_parms[0].ToString();
                // �ں�̨��ʼ����ͼƬ
                Bitmap bitmap = downloadBitmap(native_parms[0].ToString());
                if (bitmap != null)
                {
                    // ͼƬ������ɺ󻺴浽LrcCache��
                    addBitmapToMemoryCache(native_parms[0].ToString(), bitmap);

                }
      
                return bitmap;
            }

            protected override void OnPostExecute(Bitmap bitmap)
            {
                base.OnPostExecute(bitmap);
                // ����Tag�ҵ���Ӧ��ImageView�ؼ��������غõ�ͼƬ��ʾ������
                ImageView imageView = (ImageView)mPhotoWall.FindViewWithTag(imageUrl);

                if (imageView != null && bitmap != null)
                {
                    imageView.SetImageBitmap(bitmap);
                }
                taskCollection.Remove(this);
            }

            /**
             * ����HTTP���󣬲���ȡBitmap����
             * 
             * @param imageUrl
             *            ͼƬ��URL��ַ
             * @return �������Bitmap����
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