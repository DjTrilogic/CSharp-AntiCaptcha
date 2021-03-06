﻿using AntiCaptcha.CreateTask;
using AntiCaptcha.GetTask;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntiCaptcha
{
    public class AntiCaptchaLoadBalancer
    {
        private static readonly HashSet<AntiCaptchaKey> CaptchaKeys;
        private static readonly Random GetRandom;
        private static readonly ConcurrentQueue<GetTaskResponse> Queue;

        static AntiCaptchaLoadBalancer()
        {
            CaptchaKeys = new HashSet<AntiCaptchaKey>();
            Queue = new ConcurrentQueue<GetTaskResponse>();
            GetRandom = new Random();
        }

        public static bool AddKey(AntiCaptchaKey antiCaptchaKey)
        {
            lock (CaptchaKeys)
            {
                return CaptchaKeys.Add(antiCaptchaKey);
            }
        }
        public static AntiCaptchaKey GetValidAntiCaptchaKey()
        {
            List<AntiCaptchaKey> availableAntiCaptchaKey;

            lock (CaptchaKeys)
            {
                availableAntiCaptchaKey = CaptchaKeys.Where(key => key.IsReady).ToList();
            }

            if (availableAntiCaptchaKey.Count == 0)
            {
                throw new AntiCaptchaException("No valid Anti-Captca keys configured.");
            }

            var ret = availableAntiCaptchaKey[GetRandomNumber(0, availableAntiCaptchaKey.Count)];

            if (ret == null)
            {
                throw new AntiCaptchaException("No valid Anti-Captca keys configured.");
            }


            return ret;
        }

        public static async Task<GetTaskResponse> GetSolvedCaptcha(ICreateTask task)
        {
            GetTaskResponse ret;

            if (!Queue.TryDequeue(out ret))
            {
                var key = GetValidAntiCaptchaKey();
                ret = await key.GetSolvedCaptcha(task);
            }

            if (ret.UsedCount > AntiCaptchaGlobals.CaptchaRetryLimit)
            {
                var key = GetValidAntiCaptchaKey();
                ret = await key.GetSolvedCaptcha(task);
            }

            ret.IncreaseUsedCount();
            return ret;
        }



        private static int GetRandomNumber(int min, int max)
        {
            lock (GetRandom)
                return GetRandom.Next(min, max);
        }
    }
}
