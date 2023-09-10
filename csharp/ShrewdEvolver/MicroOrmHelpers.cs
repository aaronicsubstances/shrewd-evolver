using System;
using System.Collections.Generic;

namespace AaronicSubstances.ShrewdEvolver
{
    public static class MicroOrmHelpers
    {
        public static TupleItemAllocator CreateTupleItemAllocator(
            int tupleLength, int[] itemSizes,
            Func<string, int, bool> tupleIntrospector)
        {
            if (itemSizes == null)
            {
                throw new ArgumentNullException("itemSizes");
            }
            if (tupleIntrospector == null)
            {
                throw new ArgumentNullException("tupleIntrospector");
            }
            TupleItemAllocator instance = new TupleItemAllocator(
                tupleLength, itemSizes, tupleIntrospector);
            return instance;
        }

        public static class TupleItemAllocator
        {
            private readonly Func<string, int, bool> tupleIntrospector;
            private readonly int tupleLength;
            private readonly int[] itemSizes;
            private bool doingFlexibleAllocation;
            private int tupleIndex;
            private bool canProceedToNext;

            private TupleItemAllocator(
                int tupleLength, int[] itemSizes,
                Func<string, int, bool> tupleIntrospector)
            {
                this.tupleLength = tupleLength;
                this.itemSizes = itemSizes;
                this.tupleIntrospector = tupleIntrospector;
            }

            public int Allocate(int index, string name) {
                // three cases
                // 1. 3 items, 2 items, ...,
                //    - in this case just zoom into wherever
                //      index falls, and use it. 
                // if ... is just one,
                //    - in this case just use last one.
                // if ... is more than one.
                //    - in this case use state.
                if (!doingFlexibleAllocation){
                    int sum = 0;
                    for (int i = 0; i < itemSizes.length; i++) {
                        int itemSize = itemSizes[i];
                        if (sum + itemSize < index) {
                            sum += itemSize;
                        }
                        else {
                            return tupleIntrospector(
                                i, name) ? i : -1;
                        }
                    }
                    if (tupleLength - itemSizes.length < 2) {
                        return tupleIntrospector(
                            itemSizes.length, name) ?
                                itemSizes.length : -1;
                    }
                    doingFlexibleAllocation = true;
                    tupleIndex = itemSizes.length;
                    canProceedToNext = false;
                }
                return AllocateFlexible(name);
            }

            private int AllocateFlexible(string name) {
                for ( ; tupleIndex < tupleLength; tupleIndex++) {
                    if (tupleIntrospector(tupleIndex, name)) {
                        canProceedToNext = true;
                        return tupleIndex;
                    }
                    canProceedToNext = !canProceedToNext;
                    if (canProceedToNext) {
                        // meaning old value was false.
                        break;
                    }
                }
                return -1;
            }

            public void Reset() {
                doingFlexibleAllocation = false;
            }
        }

    /*
    Function which can generate all possible combinations of boolean conditions (ie 2 exponent condition count) in order to test that all variations of generated query code snippets. An example is described as follows.
     - Have an initial list of parameter lists with which to test query code generation function.
     - Assign a function to each boolean condition used within query code generation function. The function should take 2 parameters: a parameter list, and a second boolean argument indiciating whether to turn the condition on or off. Each such function should only modify the parameters which affect its condition, and even then should skip modification if those parameters already agree with the required state of the boolean condition
     - The maximum number of iterations needed will be max(size of list of parameter lists,
     2 exponent size of list of boolean condition function evaluation) or zero (if list of parameter lists is empty). Wrap around any of the two lists if iteration exhausts it.
     - Within an iteration: identify current parameter list, clone it in preparation for possible modification, and then pass it to each boolean condition function, with the second argument being the 0/1 value in the current combination (string of 0s and 1s) generated (indexed by index of the boolean condition function). Then call the query code  generation function with the final modified cloned parameter list.
     */
    
    /*
    Funtion (or canned functions) for generating SQL join clauses which take the following parameters (may have to take care of composite keys):
     - join qualifier - LEFT, INNER or OUTER (defaults to none)
     - source table - required
     - source table alias (defaults to name of table)
     - target table - required
     - target table alias (defaults to name of table)
     - source column names (defaults to primary key)
     - target column names (defaults to foreign key linked to primary key)
     - join table alias (only applies if join tables are involved, defaults to join table name)
    */
    }
}