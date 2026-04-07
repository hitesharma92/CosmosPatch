namespace CosmosPatch.Domain.Interfaces;

public interface IAppLogger
{
    bool CreateLog(string logFileName, bool append, bool showBeginHeader);
    void CloseLog(bool showEndFooter);
    void WriteMessage(string message);
    void WriteMessageOnConsole(string message);
    void BeginSection(string title);
    void EndSection(string title);
}
