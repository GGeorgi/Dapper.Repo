﻿using System;
using System.Collections.Generic;

 namespace DapperRepo.Utils
{
    public static class Extensions
    {

        public static IEnumerable<List<T>> ToChunks<T>(this List<T> self, int nSize = 100)
        {
            for (var i = 0; i < self.Count; i += nSize)
            {
                yield return self.GetRange(i, Math.Min(nSize, self.Count - i));
            }
        }
    }
}