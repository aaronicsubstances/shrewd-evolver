using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver
{
    public class LogNavigator<T> where T : ILogPositionHolder
    {
        private readonly IList<T> logs;

        public LogNavigator(IList<T> logs)
        {
            this.logs = logs;
        }

        public int NextIndex { get; private set;  } = 0;

        public bool HasNext()
        {
            return NextIndex < logs.Count;
        }

        public T Next()
        {
            return logs[NextIndex++];
        }

        public T Next(ICollection<string> searchIds)
        {
            return Next(searchIds, null);
        }

        public T Next(ICollection<string> searchIds, ICollection<string> limitIds)
        {
            if (searchIds == null)
            {
                throw new ArgumentNullException(nameof(searchIds));
            }
            int stopIndex = logs.Count;
            if (limitIds != null)
            {
                for (int i = NextIndex; i < logs.Count; i++)
                {
                    if (limitIds.Contains(logs[i].LoadPositionId()))
                    {
                        stopIndex = i;
                        break;
                    }
                }
            }
            for (int i = NextIndex; i < stopIndex; i++)
            {
                T log = logs[i];
                if (searchIds.Contains(log.LoadPositionId()))
                {
                    NextIndex = i + 1;
                    return log;
                }
            }
            return default;
        }
    }
}
