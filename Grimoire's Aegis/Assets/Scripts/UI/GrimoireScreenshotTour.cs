using System.Collections;
using System.IO;
using GrimoireAegis.Core;
using UnityEngine;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// QA helper: launch the player with  -grimoireShots &lt;folder&gt;  to walk the whole flow
    /// (story, board, puzzle, gate, battles, reveal cards, windows) and save screenshots.
    /// Does nothing in normal play.
    /// </summary>
    public class GrimoireScreenshotTour : MonoBehaviour
    {
        private string _dir;
        private GrimoireGameController _gc;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-grimoireShots")
                {
                    var go = new GameObject("GrimoireScreenshotTour");
                    DontDestroyOnLoad(go);
                    go.AddComponent<GrimoireScreenshotTour>()._dir = args[i + 1];
                    return;
                }
            }
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(_dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
        }

        private IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

        private void Skip() => _gc.TourSet("skipDialog");

        /// <summary>Waits for the question pop-up (skipping tips on the way).</summary>
        private IEnumerator UntilQuestion(float timeout = 4f)
        {
            float t = 0;
            while (_gc.battlePhase != BattlePhase.AlgorithmicChallenge && t < timeout)
            {
                if (_gc.TourDialogOpen) Skip();
                t += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator UntilPhase(BattlePhase ph, float timeout = 4f)
        {
            float t = 0;
            while (_gc.battlePhase != ph && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        private IEnumerator Start()
        {
            yield return Wait(1.2f);
            _gc = FindFirstObjectByType<GrimoireGameController>();
            if (_gc == null) { Application.Quit(); yield break; }
            var gc = _gc;

            yield return Shot("01_title");
            gc.TourSet("title", 1, 0);
            yield return Wait(0.3f);
            yield return Shot("02_difficulty");

            gc.StartNewRun(Difficulty.Medium);
            yield return Wait(1.0f);
            yield return Shot("03_story");
            Skip();
            yield return Wait(1.0f);
            yield return Shot("04_act_card");
            gc.EnterBoardFromIntro();
            yield return Wait(1.0f);
            yield return Shot("05_board_tip");
            Skip();
            yield return Wait(0.8f);
            yield return Shot("06_board");
            gc.RollExplorationDice();
            yield return Wait(2.2f);
            if (gc.TourDialogOpen) Skip();
            yield return Shot("07_after_roll");

            gc.TourSet("revealAll");
            gc.TourSet("walkTo", (int)NodeType.Elite);
            yield return Wait(1.2f);
            yield return Shot("08_map_elite");
            gc.TourSet("walkTo", (int)NodeType.Gate);
            gc.TourSet("keys", 2);
            yield return Wait(1.2f);
            yield return Shot("09_map_gate");

            gc.TourSet("puzzle");
            yield return Wait(0.5f);
            yield return Shot("10_puzzle");
            gc.AnswerPuzzle(gc.puzzleQuestion.correctIndex);
            yield return Wait(0.4f);
            yield return Shot("11_puzzle_solved");
            gc.ClosePuzzle();
            yield return Wait(0.4f);
            Skip();

            // ---- battle: correct answer ----
            gc.StartBattleEncounter("ink_zombie", false);
            yield return Wait(1.0f);
            yield return Shot("20_battle_tip");
            Skip();
            yield return Wait(0.3f);
            yield return Shot("21_battle");
            gc.ExecuteBasicAttack();
            yield return Wait(0.32f);
            yield return Shot("22_attack");
            yield return UntilQuestion();
            yield return Wait(1.7f);
            yield return Shot("23_question");
            gc.SubmitAnswer(gc.currentQuestion.correctIndex);
            yield return Wait(0.15f);
            yield return Shot("24_locked_in");
            yield return Wait(0.65f);
            yield return Shot("25_blocked");
            yield return UntilPhase(BattlePhase.TurnResolution);
            yield return Wait(0.3f);
            yield return Shot("26_reveal_correct");

            // ---- wrong answer (a sturdier monster so the fight goes on) ----
            gc.ContinueFromResolution();
            yield return Wait(0.3f);
            gc.StartBattleEncounter("bone_soldier", false);
            yield return Wait(1.0f);
            if (gc.TourDialogOpen) Skip();
            gc.ExecuteBasicAttack();
            yield return UntilQuestion();
            yield return Wait(1.7f);
            gc.SubmitAnswer((gc.currentQuestion.correctIndex + 1) % 4);
            yield return Wait(0.8f);
            yield return Shot("27_hit");
            yield return UntilPhase(BattlePhase.TurnResolution);
            yield return Wait(0.4f);
            if (gc.TourDialogOpen) { yield return Shot("28_reveal_wrong_tip"); Skip(); }
            yield return Wait(0.2f);
            yield return Shot("29_reveal_wrong");
            gc.ContinueFromResolution();
            yield return Wait(0.4f);
            gc.TourSet("spellMenu", 0);
            yield return Wait(0.2f);
            if (gc.TourDialogOpen) Skip();
            yield return Shot("30_spell_menu");

            // ---- new minions & a boss ----
            string[] mobs = { "paper_bat", "bone_soldier", "crypt_ghoul", "glitch_imp", "crystal_beetle" };
            for (int i = 0; i < mobs.Length; i++)
            {
                gc.StartBattleEncounter(mobs[i], false);
                yield return Wait(0.9f);
                if (gc.TourDialogOpen) Skip();
                gc.ExecuteBasicAttack();
                yield return UntilQuestion();
                yield return Wait(0.2f);
                yield return Shot($"4{i}_{mobs[i]}");
            }
            gc.StartBattleEncounter("the_abyss_golem", true);
            yield return Wait(1.0f);
            if (gc.TourDialogOpen) Skip();
            gc.AnalyzeMonster();
            yield return Wait(0.3f);
            yield return Shot("46_monster_info");
            gc.CloseAnalyze();

            gc.StartBattleEncounter("corrupted_slime", false);
            yield return Wait(1.0f);
            gc.monsterHp = 1;
            gc.ExecuteBasicAttack();
            yield return UntilPhase(BattlePhase.BattleWon, 5f);
            yield return Wait(0.3f);
            yield return Shot("47_victory");

            // ---- windows ----
            gc.TourSet("modal", (int)ModalOverlay.Pause); yield return Wait(0.3f); yield return Shot("50_pause");
            gc.TourSet("guide", 3); yield return Wait(0.3f); yield return Shot("51_guide_map");
            gc.TourSet("guide", 5); yield return Wait(0.3f); yield return Shot("52_guide_defend");
            gc.TourSet("codex", 1, 1); gc.TourSet("bestiaryAnim", 1); yield return Wait(0.5f); yield return Shot("53_bestiary");
            gc.TourSet("shop", 0); yield return Wait(0.3f); yield return Shot("54_shop");

            yield return Wait(0.5f);
            Application.Quit();
        }
    }
}
