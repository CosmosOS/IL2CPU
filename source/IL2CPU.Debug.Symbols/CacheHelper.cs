using System;

namespace IL2CPU.Debug.Symbols
{
    public class CacheHelper<TKey, TValue>
        where TKey: IEquatable<TKey>
    {
        public CacheHelper(Func<TKey, TValue> getValueFunc)
        {
            mGetValueFunc = getValueFunc ?? throw new ArgumentNullException(nameof(getValueFunc));
        }

        private readonly Func<TKey, TValue> mGetValueFunc;
        private TValue mCachedValue;
        private TKey mCachedKey;
        private bool mHasCachedValue = false;

        public TValue GetValue(TKey key)
        {
            if (mHasCachedValue && mCachedKey.Equals(key))
            {
                return mCachedValue;
            }

            mCachedValue = mGetValueFunc(key);
            mCachedKey = key;
            mHasCachedValue = true;
            return mCachedValue;
        }
    }
}
