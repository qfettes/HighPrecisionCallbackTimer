using System.Diagnostics;


/// <summary>
///  This class is meant to operate as a more precise alternativer to System.Threading.Timer. It creates a new thread 
///  and runs a callback on set intervals. The thread does not sleep, but is blocked instead to acheive greater precision.
///  Set active to false to stop the callback function
///  Create a new instance of the class to run the callback
///  e.g. Worker tmp = new Worker(aFunction, -1, 20, 0, true);
/// </summary>
public class Worker {
    Stopwatch processTimer;
    public bool active;
    public object connectionID;
    public double intervalMs;
    public int offsetMs;

    bool carryOverEnabled;
    double carryover;

    /// <summary>
    ///  Class constructor. Starts thread to run callback
    /// </summary>
    /// <param name="callback">The function to be called on the specified interval. Currently set to be an integer returning function</param>
    /// <param name="_connectionID">used for networking. -1 can be used if this is used for non-networking purposes</param>
    /// <param name="_intervalMs">Callback will be called every _intervalMs milliseconds</param>
    /// <param name="_initialOffset">The number of Milliseconds to wait unil the first call of the callback method. set to 0 to start immediately</param>
    /// <param name="_carryOverEnabled">
    /// When true, the Worker will try to "catch up" if it fails to call callback before _intervalMs ends. This will make subsequent intervals
    /// shorter until it is able to catch up
    /// </param>
    public Worker(Func<int> callback, object _connectionID, double _intervalMs, double _initialOffset, bool _carryOverEnabled) {
        processTimer = Stopwatch.StartNew();
        active = true;
        connectionID = _connectionID;
        intervalMs = _intervalMs;
        offsetMs = _initialOffset;
        carryOverEnabled = _carryOverEnabled;

        Thread workerThread = new Thread(DoWork);
        workerThread.Start();
    }

    public ~Worker() {
        processTimer.Stop();
    }


    /// <summary>
    ///  This is the infinite wait, function call repeat loop for the callback. Will stop if active is set to false
    /// </summary>
    private void DoWork() {
        blockThread(offsetMs); 

        while (true) {
            if (!active)
                break;

            callback();

            blockThread(intervalMs);
        }
    }

    /// <summary>
    ///  thread blocking method. uses the stopwatch to determine when to stop blocking
    /// </summary>
    private void blockThread(double durationInMs) {
        Stopwatch sw = Stopwatch.StartNew();
        double processingTime = processTimer.Elapsed.TotalMilliseconds;

        double TargetPause = durationInMs;

        if (carryOverEnabled) { //allows catch up
            double realDurationMs = TargetPause - (processingTime + carryover);
            carryover = 0.0;
            if (realDurationMs < 0.0) { //if there isn't enough pause time left after processing time, carry it to next pause. This will allow wait-to-start
                carryover = -1 * realDurationMs;
            }
        }else { // no catch up
            double realDurationMs = TargetPause - processingTime;
        }


        while (sw.Elapsed.TotalMilliseconds < realDurationMs) {
        }

        processTimer = Stopwatch.StartNew();
        sw.Stop();
    }
}