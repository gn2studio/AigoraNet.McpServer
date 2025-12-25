using AigoraNet.Common.Models;
using System.Text;

namespace AigoraNet.Common.Helpers;

public static class FileHelper
{
    private static readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// 특정 폴더 내의 모든 폴더와 파일 목록을 반환합니다.
    /// </summary>
    /// <param name="targetFolderPath">대상 폴더의 경로입니다.</param>
    /// <returns>폴더 경로와 파일 경로를 포함하는 튜플 목록을 담은 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<(IEnumerable<string> Directories, IEnumerable<string> Files)>> GetFolderAndFileListAsync(string targetFolderPath)
    {
        var result = new ReturnValues<(IEnumerable<string> Directories, IEnumerable<string> Files)>();

        if (!Directory.Exists(targetFolderPath))
        {
            result.SetError($"지정된 폴더를 찾을 수 없습니다: {targetFolderPath}");
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            var directories = Directory.EnumerateDirectories(targetFolderPath).ToList();
            var files = Directory.EnumerateFiles(targetFolderPath).ToList();
            result.SetSuccess(1, (directories, files), "폴더 및 파일 목록 조회 성공");
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 시스템 접근 권한이 없습니다: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.SetError($"폴더 및 파일 목록 조회 중 오류 발생: {ex.Message}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }

    /// <summary>
    /// 특정 파일의 내용을 읽어 문자열로 반환합니다.
    /// </summary>
    /// <param name="filePath">읽을 파일의 전체 경로입니다.</param>
    /// <returns>파일 내용의 문자열을 담은 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<string>> ReadFileAsStringAsync(string filePath)
    {
        var result = new ReturnValues<string>();

        if (!File.Exists(filePath))
        {
            result.SetError($"지정된 파일을 찾을 수 없습니다: {filePath}");
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var content = await reader.ReadToEndAsync();
                result.SetSuccess(1, content, "파일 읽기 성공");
            }
        }
        catch (IOException ex)
        {
            result.SetError($"파일 읽기 중 오류 발생: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 접근 권한이 없습니다: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.SetError($"파일 읽기 중 알 수 없는 오류 발생: {ex.Message}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }

    /// <summary>
    /// 지정된 위치에 새로운 파일을 생성하고 내용을 작성합니다.
    /// 파일이 이미 존재하면 덮어씁니다.
    /// </summary>
    /// <param name="filePath">생성할 파일의 전체 경로입니다.</param>
    /// <param name="content">파일에 작성할 내용입니다. 기본값은 빈 문자열입니다.</param>
    /// <returns>성공 여부를 나타내는 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<bool>> CreateFileAsync(string filePath, string content = "")
    {
        var result = new ReturnValues<bool>();

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            result.SetError($"파일을 생성할 디렉토리를 찾을 수 없습니다: {directory}");
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            result.SetSuccess(1, true, "파일 생성 성공");
        }
        catch (IOException ex)
        {
            result.SetError($"파일 생성 또는 쓰기 중 오류 발생: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 생성 권한이 없습니다: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.SetError($"파일 생성 중 알 수 없는 오류 발생: {ex.Message}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }

    /// <summary>
    /// 지정된 위치에 존재하는 파일의 내용을 수정합니다.
    /// 파일이 존재하지 않으면 오류를 반환합니다.
    /// </summary>
    /// <param name="filePath">수정할 파일의 전체 경로입니다.</param>
    /// <param name="newContent">파일에 새로 작성할 내용입니다.</param>
    /// <returns>성공 여부를 나타내는 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<bool>> UpdateFileAsync(string filePath, string newContent)
    {
        var result = new ReturnValues<bool>();

        if (!File.Exists(filePath))
        {
            result.SetError($"수정할 파일을 찾을 수 없습니다: {filePath}");
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(filePath, newContent, Encoding.UTF8);
            result.SetSuccess(1, true, "파일 수정 성공");
        }
        catch (IOException ex)
        {
            result.SetError($"파일 수정 중 오류 발생: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 접근 또는 쓰기 권한이 없습니다: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.SetError($"파일 수정 중 알 수 없는 오류 발생: {ex.Message}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }

    /// <summary>
    /// 지정된 위치에 존재하는 파일을 삭제합니다.
    /// </summary>
    /// <param name="filePath">삭제할 파일의 전체 경로입니다.</param>
    /// <returns>성공 여부를 나타내는 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<bool>> DeleteFileAsync(string filePath)
    {
        var result = new ReturnValues<bool>();

        if (!File.Exists(filePath))
        {
            result.SetError($"삭제할 파일을 찾을 수 없습니다: {filePath}");
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            File.Delete(filePath);
            result.SetSuccess(1, true, "파일 삭제 성공");
        }
        catch (IOException ex)
        {
            result.SetError($"파일 삭제 중 오류 발생: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 삭제 권한이 없습니다: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.SetError($"파일 삭제 중 알 수 없는 오류 발생: {ex.Message}");
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }

    /// <summary>
    /// 지정된 경로에 파일이 존재하는지 여부를 확인합니다.
    /// </summary>
    /// <param name="filePath">확인할 파일의 전체 경로입니다.</param>
    /// <returns>파일 존재 여부(bool)와 메시지를 담은 ReturnValues 객체입니다.</returns>
    public static async Task<ReturnValues<bool>> FileExistsAsync(string filePath)
    {
        var result = new ReturnValues<bool>();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            result.SetError("파일 경로가 유효하지 않습니다.");
            result.Data = false;
            return result;
        }

        await _fileAccessSemaphore.WaitAsync();
        try
        {
            bool exists = File.Exists(filePath);
            if (exists)
            {
                result.SetSuccess(1, true, "파일이 존재합니다.");
            }
            else
            {
                result.SetSuccess(0, false, "파일이 존재하지 않습니다."); // 존재하지 않을 때는 Success로, 하지만 Check는 true
            }
        }
        catch (IOException ex)
        {
            result.SetError($"파일 존재 여부 확인 중 I/O 오류 발생: {ex.Message}");
            result.Data = false;
        }
        catch (UnauthorizedAccessException ex)
        {
            result.SetError($"파일 접근 권한이 없습니다: {ex.Message}");
            result.Data = false;
        }
        catch (Exception ex)
        {
            result.SetError($"파일 존재 여부 확인 중 알 수 없는 오류 발생: {ex.Message}");
            result.Data = false;
        }
        finally
        {
            _fileAccessSemaphore.Release();
        }
        return result;
    }
}