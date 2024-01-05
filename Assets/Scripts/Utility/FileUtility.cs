using System.Text;
using UnityEngine;

public static class FileUtility
{ 
    /// <summary>
    /// 인코딩 없이 파일을 로드한다
    /// </summary>
    /// <param name="fileName"> 파일 경로 </param>
    /// <returns> 파일 내용 </returns>
    public static string LoadFile(string fileName)
    {
        if (!IsExists(fileName)) return null;

        string dataContents = BetterStreamingAssets.ReadAllText(fileName);
        return dataContents;
    }

    /// <summary>
    /// 한국어 형식에 맞게 인코딩 한 후 파일을 로드한다
    /// </summary>
    /// <param name="fileName"> 파일 경로 </param>
    /// <returns> 파일 내용 </returns>
    public static string LoadFileByKor(string fileName)
    {
        if (!IsExists(fileName)) return null;

        byte[] filedataBytes = BetterStreamingAssets.ReadAllBytes(fileName);
        string dataContents = Encoding.GetEncoding("euc-kr").GetString(filedataBytes); //혹시 모르니 한글 인코딩 포함
        return dataContents;
    }

    /// <summary>
    /// 디렉토리에 그러한 파일이 존재하는지 먼저 검사한다
    /// </summary>
    /// <param name="fileName"> 파일 경로 </param>
    /// <returns> 파일 존재 여부 </returns>
    private static bool IsExists(string fileName)
    {
        if (!BetterStreamingAssets.FileExists(fileName))
        {
            Debug.LogErrorFormat("Streaming asset not found: {0}", fileName);
            return false;
        }
        return true;
    }
}
