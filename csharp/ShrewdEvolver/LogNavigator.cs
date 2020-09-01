using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver
{
    public class LogNavigator<T> where T : ILogPositionHolder
    {
        private readonly IList<T> _logs;

        public LogNavigator(IList<T> logs)
        {
            _logs = logs;
        }

        public int NextIndex { get; private set;  } = 0;

        public bool HasNext()
        {
            return NextIndex < _logs.Count;
        }

        public T Next()
        {
            return _logs[NextIndex++];
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
            int stopIndex = _logs.Count;
            if (limitIds != null)
            {
                for (int i = NextIndex; i < _logs.Count; i++)
                {
                    if (limitIds.Contains(_logs[i].LoadPositionId()))
                    {
                        stopIndex = i;
                        break;
                    }
                }
            }
            for (int i = NextIndex; i < stopIndex; i++)
            {
                T log = _logs[i];
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
