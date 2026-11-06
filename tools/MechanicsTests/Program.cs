using HalfGripObrez;

int assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
BoltEvent Move(BoltAction action, float value) => action.MoveBolt(value, 0.9f, 0.1f);
var gun = new BoltAction();
Check(!gun.InsertRound() && !gun.Fire(), "Closed, empty action must reject loading and firing");
Check(Move(gun, 0.5f) == BoltEvent.None && !gun.Open, "Partial opening must not expose the loading gate");
Check(Move(gun, 1f) == BoltEvent.Opened && gun.Open, "Full manual pull must open the action");
for (int i = 0; i < 5; i++) Check(gun.InsertRound(), "Each of five individual cartridges must load");
Check(!gun.InsertRound() && gun.LiveRounds == 5, "Sixth cartridge must not be consumed");
Check(!gun.Chambered && !gun.Fire(), "Loading must leave the bolt open and chamber empty");
for (int frame = 0; frame < 600; frame++)
    Check(Move(gun, 1f) == BoltEvent.None && gun.Open && !gun.Chambered, "Released/open travel must never auto-close or feed");
Check(Move(gun, 0.4f) == BoltEvent.None && gun.Open && !gun.Chambered, "Partial close must not chamber");
Check(Move(gun, 0f) == BoltEvent.Closed && gun.Chambered && gun.Magazine == 4, "Manual full close must chamber exactly one");
Check(Move(gun, 0f) == BoltEvent.None && gun.LiveRounds == 5, "Resting closed must not feed repeatedly");
for (int shot = 0; shot < 5; shot++)
{
    Check(gun.Fire(), "Each chambered round must fire");
    Check(gun.SpentCase && !gun.Chambered, "Shot must leave spent brass without auto-feeding");
    Check(!gun.Fire(), "A held trigger or repeated click must not fire a second round");
    Check(Move(gun, 1f) == BoltEvent.Ejected && !gun.SpentCase, "Opening must eject exactly one spent case");
    Check(Move(gun, 1f) == BoltEvent.None, "Repeated open samples must not eject again");
    Check(Move(gun, 0f) == BoltEvent.Closed, "Every cycle requires manual closure");
}
Check(gun.LiveRounds == 0 && !gun.Fire(), "Five rounds must produce exactly five shots");
Move(gun, 1f); Check(gun.InsertRound(), "Empty action must accept another individual cartridge");
Move(gun, 0f);
Check(Move(gun, 1f) == BoltEvent.Ejected && gun.LiveRounds == 0, "Opening a live chamber must eject its round");
Check(!gun.PullOpen(), "An already-open action must not eject twice");
var loading = new BoltAction();
Move(loading, 1f);
Check(loading.ReserveRound(), "First dragged cartridge must reserve loading");
Check(!loading.ReserveRound() && !loading.InsertRound(), "Another cartridge must not enter during insertion");
Check(loading.LiveRounds == 0, "Animation must not consume the cartridge before completion");
loading.CancelRound();
Check(!loading.FinishRound(true) && loading.LiveRounds == 0, "Cancelled insertion must not consume ammo");
Check(loading.ReserveRound() && loading.FinishRound(true), "Completed insertion must accept exactly one round");
Check(!loading.FinishRound(true) && loading.LiveRounds == 1, "Duplicate completion must not duplicate ammo");
var interrupted = new BoltAction();
Move(interrupted, 1f);
Check(interrupted.ReserveRound(), "Open receiver must reserve a round");
Move(interrupted, 0f);
Check(!interrupted.FinishRound(false) && interrupted.LiveRounds == 0, "Closing during insertion must leave the physical round unconsumed");
Check(!interrupted.PendingRound, "Interrupted insertion must release the reservation");
// Garage icon uses Image.SetNativeSize; PNG resolution must not change its displayed footprint.
float ppu = PresentationMath.FitPixelDensity(160, 80, 15, 9);
Check(160 / ppu <= 15.001f && 80 / ppu <= 9.001f, "Garage icon must fit both stock footprint dimensions");
float denser = PresentationMath.FitPixelDensity(320, 160, 15, 9);
Check(Math.Abs(320 / denser - 160 / ppu) < 0.001f, "A denser icon must not grow in the garage");
Check(PresentationMath.NearReceiver(500, 300, 2, 500, 300, 8, 200), "Round and receiver at different depths must load when visually aligned");
Check(!PresentationMath.NearReceiver(550, 300, 2, 500, 300, 8, 200), "A visibly distant round must not load");
Check(!PresentationMath.NearReceiver(500, 300, -1, 500, 300, 8, 200), "A round behind the camera must not load");
float garagePpu = PresentationMath.FitPixelDensity(64, 24, PresentationMath.GarageWidth, PresentationMath.GarageHeight);
Check(Math.Abs(64 / garagePpu * 100 - 160) < 0.01f, "Independent 64px garage art must display at readable 160px width");
Check(Math.Abs(24 / garagePpu * 100 - 60) < 0.01f, "Garage icon must fit a 60px high inventory row");
Console.WriteLine($"PASS: {assertions} assertions: manual travel, open-bolt loading, five-round capacity, no auto-feed, and case ejection.");
if (args.Length > 0) GameContracts.Check(args[0]);
