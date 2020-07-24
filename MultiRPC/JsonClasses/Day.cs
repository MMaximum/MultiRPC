using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MultiRPC.JsonClasses
{
    public class Day
    {
        private CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public event EventHandler ShowProfile;

        public event EventHandler StopShowingProfile;
        
        public Day(DayOfWeek date)
        {
            Date = date;
        }

        [JsonConstructor]
        public Day(TimeSpan startTime, TimeSpan endTime, DayOfWeek date)
        {
            StartTime = startTime;
            EndTime = endTime;
            Date = date;
        }

        private TimeSpan startTime = TimeSpan.Zero;
        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                if (startTime == value)
                {
                    return;
                }

                CancellationTokenSource.Cancel();
                CancellationTokenSource = new CancellationTokenSource();

                startTime = value;
                TriggerBasedOnTime();
            }
        }

        public TimeSpan endTime = TimeSpan.Zero;
        public TimeSpan EndTime
        {
            get => endTime;
            set
            {
                if (endTime == value)
                {
                    return;
                }
                
                CancellationTokenSource.Cancel();
                CancellationTokenSource = new CancellationTokenSource();

                endTime = value;
                TriggerBasedOnTime();
            }
        }

        public readonly DayOfWeek Date;

        //TODO: See why this isn't triggering
        private async Task TriggerBasedOnTime()
        {
            if (EndTime == TimeSpan.Zero || Date == DayOfWeek.NotSet)
            {
                return;
            }

            var timeToKeepAlive = EndTime.Subtract(StartTime);
            try
            {
                var todaysDay = SysToOurDayOfWeek(DateTime.Now.DayOfWeek);
                var daysRemaining = Date >= todaysDay ? todaysDay - Date : Date - todaysDay + 7;

                //Wait until we need to trigger the... trigger or find how long the profile should last
                var timeRemaining = StartTime.Subtract(DateTime.Now.TimeOfDay.Add(TimeSpan.FromDays(daysRemaining)));
                if (timeRemaining.TotalMilliseconds > 0)
                {
                    await Task.Delay(timeRemaining, CancellationTokenSource.Token);
                }
                else
                {
                    timeToKeepAlive = timeToKeepAlive.Subtract(timeRemaining.Negate());
                }
            }
            catch (TaskCanceledException e)
            {
                App.Logging.Application("Looks like something changed while doing this profile");
                return;
            }
            
            if (timeToKeepAlive.TotalMilliseconds <= 0)
            {
                return;
            }
            
            ShowProfile?.Invoke(this, EventArgs.Empty);

            try
            {
                await Task.Delay(timeToKeepAlive, CancellationTokenSource.Token);
            }
            catch (TaskCanceledException e)
            {
                App.Logging.Application("Looks like something changed while doing this profile");
            }
            
            StopShowingProfile?.Invoke(this, EventArgs.Empty);
        }

        private DayOfWeek SysToOurDayOfWeek(System.DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            System.DayOfWeek.Monday => DayOfWeek.Monday,
            System.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
            System.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
            System.DayOfWeek.Thursday => DayOfWeek.Thursday,
            System.DayOfWeek.Friday => DayOfWeek.Friday,
            System.DayOfWeek.Saturday => DayOfWeek.Saturday,
            System.DayOfWeek.Sunday => DayOfWeek.Sunday,
            _ => DayOfWeek.NotSet
        };
    }
}