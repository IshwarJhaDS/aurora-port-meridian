using UnityEngine;

namespace PhewaValley
{
    public enum FirstStep { Move, MeetMaya, FindTeaShop, MeetLaxman, ReturnToMaya, FindCar, Drive, Park, Done }

    /// <summary>Optional hints follow real story progress; they never block controls or award money.</summary>
    public sealed class FirstVisitGuide
    {
        readonly Progress visit;
        const int Finished = 1;
        public FirstVisitGuide(Progress progress)
        {
            visit = progress;
            // Existing players who finished the opening story do not have to repeat a tutorial.
            if (visit.introVersion == 0 && visit.Mission("arrival")?.status == "completed") visit.introFlags |= Finished;
            visit.introVersion = 1;
        }
        public void Travel(float metres, bool inCar)
        {
            if (float.IsNaN(metres) || float.IsInfinity(metres) || metres <= 0 || metres > 10) return;
            if (inCar) visit.introDriveDistance = Mathf.Min(40, visit.introDriveDistance + metres);
            else visit.introWalkDistance = Mathf.Min(4, visit.introWalkDistance + metres);
        }
        public void Parked()
        {
            if (visit.Mission("arrival")?.status == "completed" && visit.introDriveDistance >= 40) visit.introFlags |= Finished;
        }
        public FirstStep Stage(bool inCar)
        {
            if ((visit.introFlags & Finished) != 0) return FirstStep.Done;
            var arrival = visit.Mission("arrival");
            if (arrival?.status == "completed") return !inCar ? FirstStep.FindCar : visit.introDriveDistance < 40 ? FirstStep.Drive : FirstStep.Park;
            int step = arrival?.step ?? 0;
            if (step == 0) return visit.introWalkDistance < 4 ? FirstStep.Move : FirstStep.MeetMaya;
            return step == 1 ? FirstStep.FindTeaShop : step == 2 ? FirstStep.MeetLaxman : FirstStep.ReturnToMaya;
        }
        public string Title(bool inCar) => Stage(inCar) switch
        {
            FirstStep.Move => "Get your bearings",
            FirstStep.MeetMaya => "A friendly face",
            FirstStep.FindTeaShop => "Follow the tea-shop route",
            FirstStep.MeetLaxman => "Namaste, Laxman dai",
            FirstStep.ReturnToMaya => "Bring the news home",
            FirstStep.FindCar => "Take your first drive",
            FirstStep.Drive => "A gentle lap of the tole",
            FirstStep.Park => "Park and step outside",
            _ => "The valley is yours"
        };
        public string Hint(bool inCar) => Stage(inCar) switch
        {
            FirstStep.Move => "WASD to walk; move the mouse to look around. Hold Shift to run. Tap Space for a hop; hold it for a full jump.",
            FirstStep.MeetMaya => "Find Maya didi in Durbar Square. Walk up to her and press E; choose a reply with 1-6 or the mouse.",
            FirstStep.FindTeaShop => "Follow the objective toward Himalayan Chiya Pasal. Stay on the footpath. M opens the map; J shows your story.",
            FirstStep.MeetLaxman => "Press E beside Laxman dai and say hello. His tea shop also offers paid shifts when you are ready.",
            FirstStep.ReturnToMaya => "Return to Maya in the square and press E. Finish the introduction to earn Rs 120 and reputation.",
            FirstStep.FindCar => "Optional driving lesson: walk to your Maruti and press E. If it is far away, press R to bring it to a nearby road.",
            FirstStep.Drive => $"W accelerates, A/D steer, B brakes. Keep left and give people room. Travel {Mathf.CeilToInt(40 - visit.introDriveDistance)} more metres.",
            FirstStep.Park => "Release W and hold B to stop in a clear spot. Press E to get out. Then choose work or a story in J.",
            _ => "Explore, meet people and choose your next story."
        };
    }
}
