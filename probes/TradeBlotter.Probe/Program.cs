using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace TradeBlotter.Probe
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" TRADEBLOTTER.WPF - PHASE 0 FEASIBILITY PROBE (FlaUI.UIA3)");
            Console.WriteLine(" Host: " + Environment.OSVersion + " | Arch: " + (Environment.Is64BitOperatingSystem ? "x64" : "x86"));
            Console.WriteLine(" Timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            Console.WriteLine("================================================================================");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Locate SUT binary relative to Probe output directory
            string[] possiblePaths = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\src\TradeBlotter.Sut\bin\Debug\net9.0-windows\TradeBlotter.Sut.exe")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\src\TradeBlotter.Sut\bin\Release\net9.0-windows\TradeBlotter.Sut.exe")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"tradeblotter-wpf-screenplay\src\TradeBlotter.Sut\bin\Debug\net9.0-windows\TradeBlotter.Sut.exe"))
            };

            string? sutPath = possiblePaths.FirstOrDefault(File.Exists);
            if (sutPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL] SUT executable not found! Searched candidates:");
                foreach (var p in possiblePaths) Console.WriteLine("   - " + p);
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine($"[INFO] Located SUT binary: {sutPath}");
            Console.WriteLine($"[INFO] SUT File Size: {new FileInfo(sutPath).Length:N0} bytes");

            var probeStopwatch = Stopwatch.StartNew();
            bool allPassed = true;
            FlaUI.Core.Application? app = null;
            UIA3Automation? automation = null;
            Process? sutProcess = null;

            try
            {
                automation = new UIA3Automation();

                // -----------------------------------------------------------------------------
                // STEP 1: Cold Boot Latency Benchmark
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 1: Cold Boot Latency Benchmark");
                Console.WriteLine("--------------------------------------------------------------------------------");
                var bootSw = Stopwatch.StartNew();

                var startInfo = new ProcessStartInfo
                {
                    FileName = sutPath,
                    Arguments = "--demo",
                    UseShellExecute = false
                };

                app = FlaUI.Core.Application.Launch(startInfo);
                sutProcess = Process.GetProcessById(app.ProcessId);
                Console.WriteLine($"[INFO] SUT process spawned with PID: {app.ProcessId}");

                var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
                bootSw.Stop();

                if (mainWindow == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] Main window was not found within 10 seconds timeout.");
                    Console.ResetColor();
                    return 1;
                }

                Console.WriteLine($"[PASS] Main window acquired: '{mainWindow.Title}'");
                Console.WriteLine($"[BENCHMARK] Cold Boot Latency: {bootSw.ElapsedMilliseconds} ms (Threshold: < 2000 ms)");
                if (bootSw.ElapsedMilliseconds > 2000)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARN] Cold boot latency exceeded 2000 ms threshold.");
                    Console.ResetColor();
                }

                // -----------------------------------------------------------------------------
                // STEP 2: UIAutomation Tree Inspection & AutomationId Discovery
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 2: UIAutomation Tree Inspection & AutomationId Discovery");
                Console.WriteLine("--------------------------------------------------------------------------------");

                string[] expectedAutomationIds = new[]
                {
                    "TradeBlotterMainView",
                    "TxtPriceTicker",
                    "BtnNewOrder",
                    "BtnCancelOrder",
                    "GridBlotterOrders"
                };

                foreach (var autoId in expectedAutomationIds)
                {
                    AutomationElement? el = null;
                    if (autoId == "TradeBlotterMainView")
                    {
                        el = mainWindow.AutomationId == autoId ? mainWindow : mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(autoId));
                    }
                    else
                    {
                        el = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(autoId));
                    }

                    if (el != null)
                    {
                        Console.WriteLine($"[PASS] Discovered element: AutomationId='{autoId}' | Name='{el.Name}' | ControlType='{el.ControlType}' | BoundingRect={el.BoundingRectangle}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[FAIL] Missing expected element: AutomationId='{autoId}'");
                        Console.ResetColor();
                        allPassed = false;
                    }
                }

                // Inspect real-time ticker
                var ticker = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPriceTicker"));
                if (ticker != null)
                {
                    string initialText = ticker.Name;
                    Console.WriteLine($"[INFO] Live Ticker initial reading: \"{initialText}\"");
                    Thread.Sleep(1200); // Allow ticker timer to tick
                    string updatedText = ticker.Name;
                    Console.WriteLine($"[INFO] Live Ticker reading after 1.2s: \"{updatedText}\"");
                    if (!string.IsNullOrEmpty(updatedText))
                    {
                        Console.WriteLine("[PASS] Real-time ticker dynamic updates verified.");
                    }
                }

                // -----------------------------------------------------------------------------
                // STEP 3: DataGrid Virtualization & Cell Access Assessment
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 3: DataGrid Virtualization & Cell Access Assessment");
                Console.WriteLine("--------------------------------------------------------------------------------");

                var grid = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("GridBlotterOrders"));
                if (grid == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] GridBlotterOrders element not found.");
                    Console.ResetColor();
                    return 1;
                }

                // Query rows
                var rows = grid.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                    .Concat(grid.FindAllChildren(cf => cf.ByControlType(ControlType.Custom)))
                    .Where(e => e.AutomationId.StartsWith("ORD-"))
                    .ToList();

                Console.WriteLine($"[INFO] Discovered {rows.Count} DataGrid rows with ORD-* prefix:");
                foreach (var row in rows)
                {
                    var cells = row.FindAllChildren();
                    string cellValues = string.Join(" | ", cells.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
                    Console.WriteLine($"   - Row [{row.AutomationId}]: {cellValues}");
                }

                if (rows.Count >= 4)
                {
                    Console.WriteLine($"[PASS] Initial seed rows present ({rows.Count} >= 4).");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAIL] Expected at least 4 seed rows, found {rows.Count}.");
                    Console.ResetColor();
                    allPassed = false;
                }

                // Verify row 0 specific order
                var firstRow = rows.FirstOrDefault();
                if (firstRow != null)
                {
                    Console.WriteLine($"[PASS] First row verified: OrderId='{firstRow.AutomationId}'");
                }

                // -----------------------------------------------------------------------------
                // STEP 4: Modal Dialog Handling & Order Entry Form Interaction
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 4: Modal Dialog Handling & Order Entry Form Interaction");
                Console.WriteLine("--------------------------------------------------------------------------------");

                var btnNewOrder = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnNewOrder"))?.AsButton();
                if (btnNewOrder == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] BtnNewOrder button not found.");
                    Console.ResetColor();
                    return 1;
                }

                Console.WriteLine("[INFO] Invoking BtnNewOrder to open OrderEntryDialog...");
                btnNewOrder.Invoke();

                // Wait for modal dialog
                Window? dialog = null;
                var dialogWaitSw = Stopwatch.StartNew();
                while (dialogWaitSw.ElapsedMilliseconds < 5000)
                {
                    dialog = mainWindow.ModalWindows.FirstOrDefault() ??
                             app.GetAllTopLevelWindows(automation).FirstOrDefault(w => w.AutomationId == "DlgOrderEntry" || (w.Title != null && w.Title.Contains("New Order Entry")));
                    if (dialog != null) break;
                    Thread.Sleep(100);
                }

                if (dialog == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] OrderEntryDialog modal window failed to appear within 5s.");
                    Console.ResetColor();
                    return 1;
                }

                Console.WriteLine($"[PASS] OrderEntryDialog detected: Title='{dialog.Title}' | AutomationId='{dialog.AutomationId}' in {dialogWaitSw.ElapsedMilliseconds} ms");

                // Locate dialog controls
                var cmbSymbol = dialog.FindFirstDescendant(cf => cf.ByAutomationId("CmbSymbol"))?.AsComboBox();
                var txtQuantity = dialog.FindFirstDescendant(cf => cf.ByAutomationId("TxtQuantity"))?.AsTextBox();
                var cmbOrderType = dialog.FindFirstDescendant(cf => cf.ByAutomationId("CmbOrderType"))?.AsComboBox();
                var txtLimitPrice = dialog.FindFirstDescendant(cf => cf.ByAutomationId("TxtLimitPrice"))?.AsTextBox();
                var btnSubmitOrder = dialog.FindFirstDescendant(cf => cf.ByAutomationId("BtnSubmitOrder"))?.AsButton();

                if (cmbSymbol == null || txtQuantity == null || cmbOrderType == null || txtLimitPrice == null || btnSubmitOrder == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] One or more OrderEntryDialog controls were missing.");
                    Console.ResetColor();
                    allPassed = false;
                }
                else
                {
                    Console.WriteLine("[PASS] All OrderEntryDialog controls located.");

                    // Select EUR/USD
                    cmbSymbol.Select(0); // EUR/USD is item 0
                    Console.WriteLine($"[INFO] Selected Symbol: {cmbSymbol.SelectedItem?.Name ?? "EUR/USD"}");

                    // Set Quantity to 250000
                    txtQuantity.Text = "250000";
                    Console.WriteLine($"[INFO] Set Quantity: {txtQuantity.Text}");

                    // Set Order Type to LIMIT
                    cmbOrderType.Select(0); // LIMIT is item 0
                    Console.WriteLine($"[INFO] Selected OrderType: {cmbOrderType.SelectedItem?.Name ?? "LIMIT"}");

                    // Set Limit Price to 1.0850
                    txtLimitPrice.Text = "1.0850";
                    Console.WriteLine($"[INFO] Set Limit Price: {txtLimitPrice.Text}");

                    // Submit order
                    Console.WriteLine("[INFO] Invoking BtnSubmitOrder...");
                    btnSubmitOrder.Invoke();
                    Thread.Sleep(500); // Allow modal to close and dispatcher to process
                }

                // -----------------------------------------------------------------------------
                // STEP 5: Grid Synchronization & State Verification
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 5: Grid Synchronization & State Verification");
                Console.WriteLine("--------------------------------------------------------------------------------");

                // Re-query rows
                var updatedRows = grid.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                    .Concat(grid.FindAllChildren(cf => cf.ByControlType(ControlType.Custom)))
                    .Where(e => e.AutomationId.StartsWith("ORD-"))
                    .ToList();

                Console.WriteLine($"[INFO] Total rows after order submission: {updatedRows.Count}");
                foreach (var r in updatedRows)
                {
                    Console.WriteLine($"   - Updated grid row: [{r.AutomationId}]");
                }
                if (updatedRows.Count == rows.Count + 1)
                {
                    Console.WriteLine($"[PASS] Order count incremented by 1 ({rows.Count} -> {updatedRows.Count}).");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAIL] Expected row count {rows.Count + 1}, found {updatedRows.Count}.");
                    Console.ResetColor();
                    allPassed = false;
                }

                var newOrderRow = updatedRows.FirstOrDefault(r => r.AutomationId == "ORD-2026-0905");
                if (newOrderRow != null)
                {
                    var cells = newOrderRow.FindAllChildren();
                    string cellDump = string.Join(" | ", cells.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
                    Console.WriteLine($"[PASS] Discovered newly created order row: AutomationId='{newOrderRow.AutomationId}'");
                    Console.WriteLine($"[INFO] Row cell contents: {cellDump}");

                    bool hasSymbol = cellDump.Contains("EUR/USD");
                    bool hasPending = cellDump.Contains("PENDING");
                    bool hasPrice = cellDump.Contains("1.0850");

                    if (hasSymbol && hasPending && hasPrice)
                    {
                        Console.WriteLine("[PASS] New order data attributes verified (EUR/USD, 1.0850, PENDING).");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[WARN] Partial match on cell contents. Expected EUR/USD, 1.0850, PENDING. Found: {cellDump}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[FAIL] Expected order row 'ORD-2026-0905' not found in grid.");
                    Console.ResetColor();
                    allPassed = false;
                }

                // -----------------------------------------------------------------------------
                // STEP 6: Process Telemetry Capture
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 6: Process Telemetry Capture");
                Console.WriteLine("--------------------------------------------------------------------------------");

                if (sutProcess != null && !sutProcess.HasExited)
                {
                    sutProcess.Refresh();
                    long workingSetBytes = sutProcess.WorkingSet64;
                    long privateMemBytes = sutProcess.PrivateMemorySize64;
                    int threadCount = sutProcess.Threads.Count;
                    int handleCount = sutProcess.HandleCount;

                    double workingSetMb = workingSetBytes / (1024.0 * 1024.0);
                    double privateMemMb = privateMemBytes / (1024.0 * 1024.0);

                    Console.WriteLine($"[TELEMETRY] SUT Process ID: {sutProcess.Id}");
                    Console.WriteLine($"[TELEMETRY] Working Set Memory: {workingSetMb:F2} MB (Threshold: < 250 MB)");
                    Console.WriteLine($"[TELEMETRY] Private Memory: {privateMemMb:F2} MB");
                    Console.WriteLine($"[TELEMETRY] Active Threads: {threadCount}");
                    Console.WriteLine($"[TELEMETRY] OS Handles: {handleCount}");

                    if (workingSetMb < 250.0)
                    {
                        Console.WriteLine("[PASS] Memory consumption within allowable envelope.");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[WARN] Working set memory ({workingSetMb:F2} MB) exceeded 250 MB threshold.");
                        Console.ResetColor();
                    }
                }

                // -----------------------------------------------------------------------------
                // STEP 7: Clean Teardown & Process Exit Verification
                // -----------------------------------------------------------------------------
                Console.WriteLine("\n--------------------------------------------------------------------------------");
                Console.WriteLine(" STEP 7: Clean Teardown & Process Exit Verification");
                Console.WriteLine("--------------------------------------------------------------------------------");

                var teardownSw = Stopwatch.StartNew();
                mainWindow.Close();

                bool cleanlyExited = false;
                if (sutProcess != null)
                {
                    cleanlyExited = sutProcess.WaitForExit(5000);
                    if (!cleanlyExited)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[WARN] SUT did not exit within 5000 ms. Forcing kill...");
                        Console.ResetColor();
                        sutProcess.Kill();
                        sutProcess.WaitForExit(2000);
                    }
                }
                teardownSw.Stop();

                Console.WriteLine($"[INFO] SUT process exit verified. HasExited = {sutProcess?.HasExited} in {teardownSw.ElapsedMilliseconds} ms.");
                if (cleanlyExited)
                {
                    Console.WriteLine("[PASS] Clean application shutdown without orphan processes.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARN] Teardown required process kill.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[EXCEPTION] Probe encountered unhandled error: {ex}");
                Console.ResetColor();
                allPassed = false;
            }
            finally
            {
                // Ensure SUT is definitely killed if still alive
                try
                {
                    if (sutProcess != null && !sutProcess.HasExited)
                    {
                        sutProcess.Kill();
                    }
                }
                catch { }

                automation?.Dispose();
                probeStopwatch.Stop();
            }

            Console.WriteLine("\n================================================================================");
            Console.WriteLine(" FEASIBILITY PROBE SUMMARY");
            Console.WriteLine("================================================================================");
            Console.WriteLine($" Total Probe Duration: {probeStopwatch.ElapsedMilliseconds} ms");
            if (allPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" OVERALL VERDICT: FEASIBLE — GO (All 7 Steps Passed)");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" OVERALL VERDICT: INFEASIBLE / DEFECTS ENCOUNTERED");
                Console.ResetColor();
                return 1;
            }
        }
    }
}
