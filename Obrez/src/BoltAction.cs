namespace HalfGripObrez
{
    internal enum BoltEvent { None, Opened, Ejected, Closed }
    // Unity-independent ammunition rules, exercised by MechanicsTests.
    internal sealed class BoltAction
    {
        internal const int Capacity = 5;
        internal bool Open { get; private set; }
        internal bool Chambered { get; private set; }
        internal bool SpentCase { get; private set; }
        internal int Magazine { get; private set; }
        internal bool PendingRound { get; private set; }
        internal int LiveRounds => Magazine + (Chambered ? 1 : 0);
        internal bool CanLoad => Open && LiveRounds < Capacity && !PendingRound;
        internal bool ReserveRound()
        {
            if (!CanLoad) return false;
            PendingRound = true;
            return true;
        }
        internal bool FinishRound(bool boltAtBack)
        {
            if (!PendingRound) return false;
            PendingRound = false;
            return boltAtBack && InsertRound();
        }
        internal void CancelRound() => PendingRound = false;
        internal BoltEvent MoveBolt(float value, float back, float forward)
        {
            if (!Open && value >= back) return PullOpen() ? BoltEvent.Ejected : BoltEvent.Opened;
            if (Open && value <= forward) { PushClosed(); return BoltEvent.Closed; }
            return BoltEvent.None;
        }
        internal bool PullOpen()
        {
            if (Open) return false;
            Open = true;
            bool eject = Chambered || SpentCase;
            Chambered = false;
            SpentCase = false;
            return eject;
        }
        internal bool InsertRound()
        {
            if (!CanLoad) return false;
            Magazine++;
            return true;
        }
        internal void PushClosed()
        {
            if (!Open) return;
            Open = false;
            if (Magazine > 0) { Magazine--; Chambered = true; }
        }
        internal bool Fire()
        {
            if (Open || !Chambered || PendingRound) return false;
            Chambered = false;
            SpentCase = true;
            return true;
        }
    }
}
