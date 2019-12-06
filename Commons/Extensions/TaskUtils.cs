using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.Commons.Extensions
{
    public static class TaskUtils
    {
        public static async Task Timeout(this Task task, TimeSpan timeout)
        {
            //http://neue.cc/2012/10/16_383.html
            //WhenAnyはどれか一つタスクが終わったら実行される
            var delay = Task.Delay(timeout);
            if (await Task.WhenAny(task, delay) == delay)
            {
                throw new TimeoutException();
            }
        }

        public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout)
        {
            await ((Task)task).Timeout(timeout);
            return await task;
        }
    }
}
