using System;
using System.Collections.Generic;

namespace AaronicSubstances.ShrewdEvolver
{
    public static class MicroOrmHelpers
    {
        public class TupleItemAllocator
        {
            private readonly int _tupleLength;
            private readonly Func<int, string, bool> _tupleIntrospector;
            private readonly Func<int, string> _columnIntrospector;
            private readonly IList<string> _endMarkers;

            // state
            private int _currEndMarkerPtr;
            private bool _doingFlexibleAllocation;
            private int _tupleIndex;
            private bool _canProceedToNext;

            /// <summary>
            /// Can be used to convert SQL result set into
            /// list of tuples of dictionaries, provided
            /// all columns for a dictionary are listed together,
            /// or columns of last dictionaries are unique.
            /// </summary>
            /// <param name="tupleLength">the number of dictionaries</param>
            /// <param name="tupleIntrospector">function which
            /// determines whether a column name exists in
            /// dictionary at a specified index.</param>
            /// <param name="columnIntrospector">function which
            /// specifies the name at a given column index.</param>
            /// <param name="endMarkers">Can be used
            /// generate demarcate dictionaries into first and last sections,
            /// where the first section are those whose names appear 
            /// before entries in end markers, and the last
            /// section are those whose names appear before all the
            /// end markers</param>
            public TupleItemAllocator(
                int tupleLength,
                Func<int, string, bool> tupleIntrospector,
                Func<int, string> columnIntrospector,
                IList<string> endMarkers)
            {
                if (tupleIntrospector == null)
                {
                    throw new ArgumentNullException(nameof(tupleIntrospector));
                }
                if (columnIntrospector == null)
                {
                    throw new ArgumentNullException(nameof(columnIntrospector));
                }
                _tupleLength = tupleLength;
                _tupleIntrospector = tupleIntrospector;
                _columnIntrospector = columnIntrospector;
                _endMarkers = endMarkers;
            }

            public int Allocate(int index)
            {
                string colName = _columnIntrospector(index);
                if (!_doingFlexibleAllocation)
                {
                    while (_endMarkers != null && _currEndMarkerPtr < _endMarkers.Count)
                    {
                        // treat all indices for end markers as being
                        // greater than zero.
                        if (index == 0 || colName != _endMarkers[_currEndMarkerPtr])
                        {
                            if (_currEndMarkerPtr < _tupleLength)
                            {
                                return _currEndMarkerPtr;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                        _currEndMarkerPtr++;
                    }
                    _doingFlexibleAllocation = true;
                    _tupleIndex = _currEndMarkerPtr;
                    _canProceedToNext = false;
                }
                for (; _tupleIndex < _tupleLength; _tupleIndex++)
                {
                    if (_tupleIntrospector(_tupleIndex, colName))
                    {
                        _canProceedToNext = true;
                        return _tupleIndex;
                    }
                    if (!_canProceedToNext)
                    {
                        _canProceedToNext = true;
                        break;
                    }
                    _canProceedToNext = false;
                }
                return -1;
            }
        }

    /*
    Test Helper function which can generate all possible combinations of boolean conditions (ie 2 exponent condition count) in order to test that all variations of generated query code snippets. An example is described as follows.
     - Have an initial list of parameter lists with which to test query code generation function.
     - Assign a function to each boolean condition used within query code generation function. The function should take 2 parameters: a parameter list, and a second boolean argument indiciating whether to turn the condition on or off. Each such function should only modify the parameters which affect its condition, and even then should skip modification if those parameters already agree with the required state of the boolean condition
     - The maximum number of iterations needed will be max(size of list of parameter lists,
     2 exponent size of list of boolean condition function evaluation) or zero (if list of parameter lists is empty). Wrap around any of the two lists if iteration exhausts it.
     - Within an iteration: identify current parameter list, clone it in preparation for possible modification, and then pass it to each boolean condition function, with the second argument being the 0/1 value in the current combination (string of 0s and 1s) generated (indexed by index of the boolean condition function). Then call the query code  generation function with the final modified cloned parameter list.
     */
    
    /*
    Code Generation Script funtion (or canned functions) for generating SQL join clauses which take the following parameters (may have to take care of composite keys):
     - join qualifier - LEFT, INNER or OUTER (defaults to none)
     - source table - required
     - source table alias (defaults to name of table)
     - target table - required
     - target table alias (defaults to name of table)
     - source column names (defaults to primary key)
     - target column names (defaults to foreign key linked to primary key)
     - join table alias (only applies if join tables are involved, defaults to join table name)
    */
    /*
     * Join clause generation code can be generalized to picking
     * the shortest join route between entities from a ERD graph
     * which are not directly connected via a foreign key or
     * join table.
     */
    }
}