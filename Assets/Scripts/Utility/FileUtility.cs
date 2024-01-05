using System.Text;
using UnityEngine;

public static class FileUtility
{ 
    /// <summary>
    /// ���ڵ� ���� ������ �ε��Ѵ�
    /// </summary>
    /// <param name="fileName"> ���� ��� </param>
    /// <returns> ���� ���� </returns>
    public static string LoadFile(string fileName)
    {
        if (!IsExists(fileName)) return null;

        string dataContents = BetterStreamingAssets.ReadAllText(fileName);
        return dataContents;
    }

    /// <summary>
    /// �ѱ��� ���Ŀ� �°� ���ڵ� �� �� ������ �ε��Ѵ�
    /// </summary>
    /// <param name="fileName"> ���� ��� </param>
    /// <returns> ���� ���� </returns>
    public static string LoadFileByKor(string fileName)
    {
        if (!IsExists(fileName)) return null;

        byte[] filedataBytes = BetterStreamingAssets.ReadAllBytes(fileName);
        string dataContents = Encoding.GetEncoding("euc-kr").GetString(filedataBytes); //Ȥ�� �𸣴� �ѱ� ���ڵ� ����
        return dataContents;
    }

    /// <summary>
    /// ���丮�� �׷��� ������ �����ϴ��� ���� �˻��Ѵ�
    /// </summary>
    /// <param name="fileName"> ���� ��� </param>
    /// <returns> ���� ���� ���� </returns>
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
