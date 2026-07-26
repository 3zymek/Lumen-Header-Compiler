using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal static class StringHasher {

    private const uint mOffsetBasis = 2166136261U;
    private const uint mPrime = 16777619U;

    public static uint Hash( byte[] data ) {

        unchecked {

            uint hash = mOffsetBasis;
            foreach (byte b in data) {
                hash ^= b;
                hash *= mPrime;
            }
            return hash;

        }

    }

    public static uint Hash(string value) {
        return Hash( Encoding.UTF8.GetBytes( value ) );
    }

}
