using CriCpkMaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YAMMR
{
    internal class CPK
    {

        public static void Unpack(string filePath)
        {
            var cpkMaker = new CpkMaker();

            if (!cpkMaker.AnalyzeCpkFile(filePath))
                return;

            var outDirName = Regex.Replace(Path.GetFullPath(filePath), ".([^\\.]+)$", "_$1");
            Directory.CreateDirectory(outDirName);
            cpkMaker.StartToExtract(outDirName);
            cpkMaker.WaitForComplete();
        

        }












    }
}
