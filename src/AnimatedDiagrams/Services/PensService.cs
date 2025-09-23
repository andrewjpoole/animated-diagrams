using AnimatedDiagrams.Models;

namespace AnimatedDiagrams.Services
{
    public class PensService
    {
        public List<PenModel> Pens { get; private set; } = new();
        public Guid? ActivePenId { get; private set; }
        public PenModel? ActivePen => ActivePenId.HasValue ? Pens.FirstOrDefault(p => p.PenId == ActivePenId.Value) : Pens.FirstOrDefault();
        public event Action? Changed;

        public void SetPens(List<PenModel> pens)
        {
            Pens = pens;
            // If the current ActivePenId is not present, reset to first pen
            if (ActivePenId == null || !Pens.Any(p => p.PenId == ActivePenId))
                ActivePenId = Pens.FirstOrDefault()?.PenId;
            Changed?.Invoke();
        }

        public void SetActivePen(Guid penId)
        {
            if (Pens.Any(p => p.PenId == penId))
            {
                ActivePenId = penId;
                Changed?.Invoke();
            }
        }

        public void CycleActivePen()
        {
            if (Pens.Count == 0) return;
            if (ActivePenId == null)
            {
                ActivePenId = Pens.First().PenId;
            }
            else
            {
                var idx = Pens.FindIndex(p => p.PenId == ActivePenId);
                var nextIdx = (idx + 1) % Pens.Count;
                ActivePenId = Pens[nextIdx].PenId;
            }
            Changed?.Invoke();
        }

        public void LoadFromList(List<PenModel> pens)
        {
            Pens = pens;
            if (ActivePenId == null || !Pens.Any(p => p.PenId == ActivePenId))
                ActivePenId = Pens.FirstOrDefault()?.PenId;
        }

        public int GetActivePenIndex()
        {
            if (ActivePenId == null) return -1;
            return Pens.FindIndex(p => p.PenId == ActivePenId);
        }
    }
}
