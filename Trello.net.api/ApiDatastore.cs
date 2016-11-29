using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Trello.net.api
{
    public class ApiDatastore
    {
        public string RootFolder { get; set; }

        public bool TryLoadBoardAnalysis(string boardId, DateTime start, DateTime end, TimeGranularity granularity,
            out BoardAnalysisResult analysisResult)
        {
            var path = getPath(boardId, start, end, granularity);
            return File.Exists(path) 
                ? tryLoadAndDeserializeBoardAnalysis(path, out analysisResult) 
                : tryLoadLastBoardAnalysis(boardId, start, granularity, out analysisResult);
        }

        private bool tryLoadLastBoardAnalysis(string boardId, DateTime start, TimeGranularity granularity,
            out BoardAnalysisResult analysisResult)
        {
            analysisResult = null;
            var dir = new DirectoryInfo(RootFolder);
            if (!dir.Exists)
                return false;

            var files = dir.GetFiles(getFilePattern(boardId, start, granularity)).ToList();
            if (files.Count == 0)
                return false;

            files.Sort((f1, f2) => string.CompareOrdinal(f1.Name, f2.Name));
            var path = Path.Combine(RootFolder, files.Last().Name);
            return tryLoadAndDeserializeBoardAnalysis(path, out analysisResult);
        }

        private bool tryLoadAndDeserializeBoardAnalysis(string path, out BoardAnalysisResult analysisResult)
        {
            var json = File.ReadAllText(path);
            try
            {
                analysisResult = JsonConvert.DeserializeObject<BoardAnalysisResult>(json);
                analysisResult.LinkPeriods();
                return true;
            }
            catch (Exception ex)
            {
                analysisResult = null;
                return false;
            }
        }


        public void SaveBoardAnalysis(BoardAnalysisResult analysisResult)
        {
            var json = JsonConvert.SerializeObject(analysisResult);
            var path = getPath(analysisResult);
            File.WriteAllText(path, json);
        }

        private string getPath(BoardAnalysisResult result)
        {
            return getPath(result.BoardId, result.Start, result.End, result.Granularity);
        }

        private string getPath(string boardId, DateTime start, DateTime end, TimeGranularity granularity)
        {
            return Path.Combine(RootFolder, $"{boardId}_{start:yy-MM-dd}_{end:yy-MM-dd}_{granularity}.json");
        }

        private string getFilePattern(string boardId, DateTime start, TimeGranularity granularity)
        {
            return $"{boardId}_{start:yy-MM-dd}_*_{granularity}.json";
        }

        public ApiDatastore(string rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder)) throw new ArgumentNullException(nameof(rootFolder));
            RootFolder = rootFolder;
        }
    }
}
