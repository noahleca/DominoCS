using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Text;

namespace WebSocket1.Models
{
    public static class Game
    {
        public static string[,] getVirtualTiles()
        {
            string[,] arr2 = new string[7, 7];
            byte[] arr = Encoding.Unicode.GetBytes("\U0001F031");
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    string ficha = Encoding.Unicode.GetString(arr);
                    if (i == j)
                    {
                        arr[2] += 50;
                        ficha = Encoding.Unicode.GetString(arr);
                        arr[2] -= 50;
                    }
                    arr2[i, j] = ficha;
                    arr[2]++;
                }
            }
            return arr2;
        }

        public static List<string> getRealTiles()
        {
            string[,] fichasVirtuals = getVirtualTiles();
            List<string> fichasReals = new List<string>();
            int k = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (i >= j)
                    {
                        fichasReals.Add(fichasVirtuals[i, j]);
                        k++;
                    }
                }
            }
            return fichasReals;
        }
        public static List<string> getRandomRealTiles()
        {
            List<string> sortedTiles = getRealTiles();

            Random random = new Random();
            List<string> randomTiles = new List<string>();

            while (sortedTiles.Count > 0)
            {
                int index = random.Next(sortedTiles.Count);
                randomTiles.Add(sortedTiles[index]);
                sortedTiles.RemoveAt(index);
            }
            return randomTiles;
        }
    }
}