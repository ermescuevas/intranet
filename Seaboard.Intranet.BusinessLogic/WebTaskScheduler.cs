using System;
using System.Collections;
using System.Web;
using System.Web.Caching;
using System.Globalization;
namespace Seaboard.Intranet.BusinessLogic
{
    /// <summary>
    /// Specfies the item execute period in ordered intervals.
    /// </summary>
    public enum TaskExecutePeriod
    {
        /// <summary>
        /// Executes the task immediately
        /// </summary>
        None,

        /// <summary>
        /// Executes the task every noon
        /// </summary>
        EveryNoon,

        /// <summary>
        /// Executes the task every night
        /// </summary>
        Daily,

        /// <summary>
        /// Executes the task every weekend at night
        /// </summary>
        Weekly,

        /// <summary>
        /// Executes the task every two weeks at night
        /// </summary>
        TwoWeekly,

        /// <summary>
        /// Executes the task every month at night
        /// </summary>
        Monthly,

        /// <summary>
        /// Executes the task every week at night using Persian dates
        /// </summary>
        ShamsiWeekly,

        /// <summary>
        /// Executes the task every two weeks at night using Persian dates
        /// </summary>
        ShamsiTwoWeekly,

        /// <summary>
        /// Executes the task every month at night using Persian dates
        /// </summary>
        ShamsiMonthly
    };

    /// <summary>
    /// Specifies the implemention of call back method.
    /// </summary>
    /// <param name="e"></param>
    public delegate void WebTaskExecuteCallback(WebTaskEventArgs e);

    /// <summary>
    /// Specfies the web task item activity mode.
    /// </summary>
    public enum TaskActivityType
    {
        /// <summary>
        /// Period typed execution selected
        /// </summary>
        Period,

        /// <summary>
        /// Custom
        /// </summary>
        Custom,

        /// <summary>
        /// Custom daily execution
        /// </summary>
        DailyCustom
    };


    /// <summary>
    /// Schedule your application tasks in web with WebTaskScheduler
    /// </summary>
    public static class WebTaskScheduler
    {
        /// <summary>
        /// Lock state of WebTaskScheduler.
        /// </summary>
        private enum WebTaskState
        {
            /// <summary>
            /// Static instance is on lock mode
            /// </summary>
            Locked,

            /// <summary>
            /// Static instance is unlock
            /// </summary>
            Unlocked
        }


        #region variables
        private static readonly string CachePrefixKey = "WebTaskScheduledJob_" + DateTime.Now.Ticks + ";";
        private static readonly Hashtable CacheDataList = new Hashtable();
        private static WebTaskState _lockSate = WebTaskState.Unlocked;
        private const int WaitForUnlockSecounds = 20;
        #endregion

        #region properties
        /// <summary>
        /// Current application cache store!
        /// </summary>
        private static Cache Cache
        { get { return HttpRuntime.Cache; } }

        /// <summary>
        /// Wait seconds for WaitForUnlock method
        /// </summary>
        internal static int WaitSecounds
        { get { return WaitForUnlockSecounds; } }
        #endregion

        #region Public static methods

        /// <summary>
        /// Gets a web task item by related key
        /// </summary>
        public static WebTaskItem GetItem(string key)
        {
            return (WebTaskItem)CacheDataList[key];
        }


        /// <summary>
        /// Ensures all items are ready.
        /// </summary>
        internal static void EnsureItemsAvailable()
        {
            // It seems we don't need this!
            //WebTaskItem item;
            //if (HttpContext.Current != null)
            //{
            //    foreach (DictionaryEntry entry in _CacheDataList)
            //    {
            //        item = (WebTaskItem)entry.Value;
            //        if (item.Lunched)
            //        {
            //            item.Lunched = false;
            //            RevivalTaskItem(item);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Adds a web task to the tasks queue with execute period method
        /// </summary>
        /// <param name="key">A key to access this item</param>
        /// <param name="callback">Web task callback method</param>
        /// <param name="period">Execution period type</param>
        /// <exception cref="System.Data.DuplicateNameException">Throws System.Data.DuplicateNameException when specified key is duplicate.</exception>
        public static void Add(string key, WebTaskExecuteCallback callback, TaskExecutePeriod period)
        {
            CheckKeyExisting(key);

            var item = new WebTaskItem();
            item.CallBack = callback;
            item.Key = key;
            item.Period = period;
            item.ExecuteDate = CalulatePeriodExecuteTimeFromNow(DateTime.MaxValue, period);
            item._ActivationType = TaskActivityType.Period;
            item.DaysPeriod = 0;
            InternalAdd(item);
        }

        /// <summary>
        /// Adds a web task to the tasks queue with days period method
        /// </summary>
        /// <param name="key">A key to access this item</param>
        /// <param name="callback">Web task callback method instance</param>
        /// <param name="daysPeriod">Days interval</param>
        /// <exception cref="System.Data.DuplicateNameException">Throws System.Data.DuplicateNameException when specified key is duplicate.</exception>
        public static void Add(string key, WebTaskExecuteCallback callback, int daysPeriod)
        {
            CheckKeyExisting(key);

            var item = new WebTaskItem();
            item.CallBack = callback;
            item.Key = key;
            item.Period = TaskExecutePeriod.None;
            item.ExecuteDate = CalulateDailyPeriodExecutionFromNow(DateTime.MaxValue, daysPeriod);
            item.DaysPeriod = daysPeriod;
            item._ActivationType = TaskActivityType.DailyCustom;
            InternalAdd(item);
        }

        /// <summary>
        /// Adds a web task to the tasks queue with days period method
        /// </summary>
        /// <param name="key">A key to access this item</param>
        /// <param name="callback">Web task callback method instance</param>
        /// <exception cref="System.Data.DuplicateNameException">Throws System.Data.DuplicateNameException when specified key is duplicate.</exception>
        public static void Add(string key, WebTaskExecuteCallback callback, TimeSpan customPeriod)
        {
            CheckKeyExisting(key);

            var item = new WebTaskItem
            {
                CallBack = callback,
                Key = key,
                Period = TaskExecutePeriod.None,
                ExecuteDate = CalulateCustomExecutionFromNow(DateTime.MaxValue, customPeriod),
                CustomPeriod = customPeriod,
                _ActivationType = TaskActivityType.Custom
            };
            InternalAdd(item);
        }

        /// <summary>
        /// Remove a web task from queue
        /// </summary>
        public static void Remove(WebTaskItem item)
        {
            Remove(item.Key);
        }

        /// <summary>
        /// Remove a web task from queue by specified key
        /// </summary>
        public static void Remove(string key)
        {
            Cache.Remove(GenerateCacheKey(key));
            CacheDataList.Remove(key);
        }

        /// <summary>
        /// Lock access to tasks queue
        /// </summary>
        public static void Lock()
        {
            Lock(true);
        }

        /// <summary>
        /// Lock access to tasks queue with wait option
        /// </summary>
        public static void Lock(bool wait)
        {
            if (wait)
                if (!WaitForUnlock())
                    throw new Exception("Waiting for unlock timed out.");
            _lockSate = WebTaskState.Locked;
        }

        /// <summary>
        /// Unlock access to tasks queue
        /// </summary>
        public static void Unlock()
        {
            _lockSate = WebTaskState.Unlocked;

        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Adds a web task to the tasks queue
        /// </summary>
        internal static void InternalAdd(WebTaskItem item)
        {
            CacheDataList.Add(item.Key, item);
            InternalInsertCache(item);
        }

        /// <summary>
        /// Waits until task unlock
        /// </summary>
        internal static bool WaitForUnlock()
        {
            var waitMSecCount = 1;
            var waitSecCount = 1;
            while (_lockSate == WebTaskState.Locked)
            {
                System.Threading.Thread.Sleep(200);
                waitMSecCount++;
                if (waitMSecCount >= 10)
                {
                    waitSecCount++;
                    waitMSecCount = 1;
                }
                if (waitSecCount >= WaitForUnlockSecounds)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Defines a callback method for notifying applications when a cached item is removed from the Cache.
        /// </summary>
        /// <param name="key">The key that is removed from the cache</param>
        /// <param name="value">The Object item associated with the key removed from the cache.</param>
        /// <param name="reason">The reason the item was removed from the cache.</param>
        private static void __CacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            var orgkey = RemoveCacheKey(key);
            var item = (WebTaskItem)CacheDataList[orgkey];
            var evntArgs = new WebTaskEventArgs(item, true);
            if (item != null)
            {
                // Continue only if cache item expired
                if (item.CallBack != null && reason != CacheItemRemovedReason.Removed)
                {
                    // Call callback instance
                    item.CallBack(evntArgs);

                    // Martk task as executed OR removed it
                    SetTaskItemContinue(evntArgs.TaskItem, evntArgs.CanContinue);

                    // if user request to continue executionof this task
                    if (evntArgs.CanContinue)
                        RevivalTaskItem(evntArgs.TaskItem);
                }
            }
        }

        /// <summary>
        /// Check for key duplication.
        /// </summary>
        private static void CheckKeyExisting(string key)
        {
            //string taskKey = generateCacheKey(key);
            if (CacheDataList.ContainsKey(key))
                throw new System.Data.DuplicateNameException("Specified key '" + key + "' already exists.");
        }

        /// <summary>
        /// Revival executed task item.
        /// </summary>
        /// <param name="item">Executed task item</param>
        private static void RevivalTaskItem(WebTaskItem item)
        {
            item.Lunched = false;
            ReCalulateExecutionDate(item);
            InternalInsertCache(item);
        }

        /// <summary>
        /// Set the next next execution date from now
        /// </summary>
        private static void ReCalulateExecutionDate(WebTaskItem item)
        {
            switch (item.ActivationType)
            {
                case TaskActivityType.Period:
                    item.ExecuteDate = CalulatePeriodExecuteTimeFromNow(item.LastExecute, item.Period);
                    break;
                case TaskActivityType.DailyCustom:
                    item.ExecuteDate = CalulateDailyPeriodExecutionFromNow(item.LastExecute, item.DaysPeriod);
                    break;
                case TaskActivityType.Custom:
                    item.ExecuteDate = CalulateCustomExecutionFromNow(item.LastExecute, item.CustomPeriod);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static DateTime CalulateDailyPeriodExecutionFromNow(DateTime lastExecute, int daysPeriod)
        {
            //bool hasLastExecute = (lastExecute != DateTime.MaxValue);
            //return DateTime.Now.AddDays(daysPeriod);

            var resultHour = 23;
            var resultMinute = 59;

            var result = DateTime.Now;

            result = result.AddDays(daysPeriod);
            result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        private static DateTime CalulateCustomExecutionFromNow(DateTime lastExecute, TimeSpan timeSpan)
        {
            if (lastExecute != DateTime.MaxValue && lastExecute != DateTime.MinValue)
                return lastExecute.AddSeconds(timeSpan.TotalSeconds);
            else
                return DateTime.Now.AddSeconds(timeSpan.TotalSeconds);
        }

        /// <summary>
        /// 
        /// </summary>
        private static DateTime CalulatePeriodExecuteTimeFromNow(DateTime lastExecute, TaskExecutePeriod period)
        {
            var result = DateTime.Now;
            int resultDay;
            var hasLastExecute = (lastExecute != DateTime.MaxValue);

            // Execute task at hour 23:59:0
            var resultHour = 23;
            const int resultMinute = 59;

            switch (period)
            {
                case TaskExecutePeriod.None:
                    result = DateTime.Now;
                    break;

                case TaskExecutePeriod.EveryNoon:
                    // Execution should be on next day on 1 hour on noon of day
                    resultHour = 13;
                    result = DateTime.Now;

                    if (resultHour <= result.Hour)
                        result = result.AddDays(1);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.Daily:
                    result = DateTime.Now;
                    if (resultHour <= result.Hour)
                        result = result.AddDays(1);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.Weekly:
                    result = DateTime.Now;
                    result = result.AddDays(1);
                    var weekLastDay = -((int)result.DayOfWeek + 1);
                    result = result.AddDays(weekLastDay);
                    result = result.AddDays(7);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.ShamsiWeekly:
                    result = DateTime.Now;
                    result = result.AddDays(1);
                    var shamsiWeekLastDay = -(GetShamsiDayOfWeek(result.DayOfWeek) + 1);
                    result = result.AddDays(shamsiWeekLastDay);
                    result = result.AddDays(7);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                //=======

                case TaskExecutePeriod.TwoWeekly:
                    result = DateTime.Now;
                    result = result.AddDays(1);
                    var twoWeekLastDay = -((int)result.DayOfWeek + 1);
                    result = result.AddDays(twoWeekLastDay);
                    result = result.AddDays(14);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.ShamsiTwoWeekly:
                    result = DateTime.Now;
                    result = result.AddDays(1);
                    var shamsiTwoWeekLastDay = -(GetShamsiDayOfWeek(result.DayOfWeek) + 1);
                    result = result.AddDays(shamsiTwoWeekLastDay);
                    result = result.AddDays(14);

                    result = new DateTime(result.Year, result.Month, result.Day, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.ShamsiMonthly:
                    result = DateTime.Now;
                    result = result.AddDays(-(result.Day));

                    var calendar = new PersianCalendar();
                    result = calendar.AddMonths(result, 1);

                    resultDay = DateTime.DaysInMonth(result.Year, result.Month);

                    result = new DateTime(result.Year, result.Month, resultDay, resultHour, resultMinute, 0);
                    break;

                case TaskExecutePeriod.Monthly:
                    result = DateTime.Now;
                    result = result.AddDays(-(result.Day));
                    result = result.AddMonths(1);
                    resultDay = DateTime.DaysInMonth(result.Year, result.Month);

                    result = new DateTime(result.Year, result.Month, resultDay, resultHour, resultMinute, 0);
                    break;
            }
            return result;
        }

        private static bool IsDateDaysLargerThan(DateTime src, int destDay, int destHour, int destMin, int destSec)
        {
            if (src.Day > destDay)
                return true;
            else if (src.Day == destDay && src.Hour > destHour)
                return true;
            else if (src.Day == destDay && src.Hour == destHour && src.Minute > destMin)
                return true;
            else if (src.Day == destDay && src.Hour == destHour && src.Minute == destMin && src.Second > destSec)
                return true;
            else
                return false;
        }

        private static int GetShamsiDayOfWeek(DayOfWeek dayWeek)
        {
            switch ((int)dayWeek)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    return (int)dayWeek + 1;
                case 6:
                    return 0;
                default:
                    return (int)dayWeek;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static string GenerateCacheKey(string orgkey)
        {
            return CachePrefixKey + orgkey;
        }


        /// <summary>
        /// 
        /// </summary>
        private static string RemoveCacheKey(string cachekey)
        {
            if (cachekey.StartsWith(CachePrefixKey))
                return cachekey.Remove(0, CachePrefixKey.Length);
            else
                return cachekey;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void SetTaskItemContinue(WebTaskItem item, bool Continue)
        {
            if (Continue)
            {
                item.Lunched = true;
                item.LastExecute = DateTime.Now;
            }
            else
            {
                CacheDataList.Remove(item.Key);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void InternalInsertCache(WebTaskItem item)
        {
            var taskKey = GenerateCacheKey(item.Key);
            Cache.Remove(taskKey);
            Cache.Insert(taskKey, DateTime.Now.ToString(), null, item.ExecuteDate, TimeSpan.Zero, CacheItemPriority.NotRemovable, __CacheItemRemoved);
        }
        #endregion
    }

    /// <summary>
    /// Informations about web task
    /// </summary>
    public sealed class WebTaskItem
    {
        #region variables
        internal TaskExecutePeriod _Period = TaskExecutePeriod.None;
        internal WebTaskExecuteCallback _CallBack;
        internal TaskActivityType _ActivationType = TaskActivityType.DailyCustom;
        internal string _Key;
        internal int _DaysPeriod = 1;
        internal DateTime _ExecuteDate = DateTime.Now;
        internal DateTime _LastExecute = DateTime.MaxValue;
        internal bool _Lunched = false;
        internal TimeSpan _CustomPeriod;
        #endregion

        #region properties
        public TaskExecutePeriod Period
        {
            get { return _Period; }
            set { _Period = value; }
        }
        public int DaysPeriod
        {
            get { return _DaysPeriod; }
            set { _DaysPeriod = value; }
        }
        public WebTaskExecuteCallback CallBack
        {
            get { return _CallBack; }
            set { _CallBack = value; }
        }
        public DateTime LastExecute
        {
            get { return _LastExecute; }
            set { _LastExecute = value; }
        }
        public TaskActivityType ActivationType
        {
            get { return _ActivationType; }
            //set { _ActivationType = value; }
        }
        public TimeSpan CustomPeriod
        {
            get { return _CustomPeriod; }
            set { _CustomPeriod = value; }
        }
        internal string Key
        {
            get { return _Key; }
            set { _Key = value; }
        }
        internal DateTime ExecuteDate
        {
            get { return _ExecuteDate; }
            set { _ExecuteDate = value; }
        }
        internal bool Lunched
        {
            get { return _Lunched; }
            set { _Lunched = value; }
        }

        #endregion
    }

    /// <summary>
    /// Web task callback event args
    /// </summary>
    public sealed class WebTaskEventArgs : EventArgs
    {
        public readonly WebTaskItem TaskItem;
        public bool CanContinue = true;
        public WebTaskEventArgs(WebTaskItem taskitem, bool Continue)
        {
            CanContinue = Continue;
            TaskItem = taskitem;
        }
    }
}