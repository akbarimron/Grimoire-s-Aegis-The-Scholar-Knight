using System.Collections;
using System.IO;
using GrimoireAegis.Core;
using UnityEngine;

namespace GrimoireAegis.UI
{
    /// <summary>
    /// QA helper: launch the player with  -grimoireShots &lt;folder&gt;  to walk every screen and
    /// save screenshots (title, difficulty, act card, board, battle beats, guide, bestiary, shop...).
    /// Does nothing in normal play.
    /// </summary>
    public class GrimoireScreenshotTour : MonoBehaviour
    {
        private string _dir;

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

        private IEnumerator Start()
        {
            yield return Wait(1.2f);
            var gc = FindFirstObjectByType<GrimoireGameController>();
            if (gc == null) { Application.Quit(); yield break; }

            yield return Shot("01_title");
            gc.TourSet("title", 1, 0);
            yield return Wait(0.3f);
            yield return Shot("02_difficulty");

            gc.StartNewRun(Difficulty.Medium, 1);
            yield return Wait(1.2f);
            yield return Shot("03_act_intro");
            gc.EnterBoardFromIntro();
            gc.RollExplorationDice();
            yield return Wait(0.9f);
            yield return Shot("04_board");

            // ---- Medium battle: attack, defend pop-up, Perfect Guard ----
            gc.StartBattleEncounter("skeleton_archer", false);
            yield return Wait(0.25f);
            yield return Shot("10_spawn");
            yield return Wait(0.8f);
            gc.TourSet("status", 0); gc.TourSet("panic", 1);
            yield return Shot("11_battle_command");
            gc.ExecuteBasicAttack();
            yield return Wait(0.32f);
            yield return Shot("12_attack_hit");
            yield return Wait(0.9f);
            yield return Shot("13_question_reading");
            yield return Wait(1.6f);
            yield return Shot("14_question_timer");
            gc.SubmitAnswer(gc.currentQuestion.correctIndex);
            yield return Wait(0.35f);
            yield return Shot("15_perfect_guard");
            yield return Wait(0.6f);
            yield return Shot("16_resolution");
            gc.ContinueFromResolution();   // arrow 2
            yield return Wait(1.0f);
            gc.SubmitAnswer((gc.currentQuestion.correctIndex + 1) % 4);
            yield return Wait(0.3f);
            yield return Shot("17_break");
            yield return Wait(0.6f);
            yield return Shot("18_break_resolution");
            gc.ContinueFromResolution();   // arrow 3
            yield return Wait(1.0f);
            gc.SubmitAnswer(gc.currentQuestion.correctIndex);
            yield return Wait(0.8f);
            gc.ContinueFromResolution();
            yield return Wait(0.4f);
            gc.TourSet("spellMenu", 2);
            yield return Shot("19_spell_menu");

            // ---- Easy boss battle: no timer, enrage ----
            gc.difficulty = Difficulty.Easy;
            gc.StartBattleEncounter("the_abyss_golem", true);
            yield return Wait(1.0f);
            gc.AnalyzeMonster();
            yield return Wait(0.4f);
            yield return Shot("20_analyze");
            gc.CloseAnalyze();
            yield return Wait(1.4f);
            yield return Shot("21_easy_question");
            gc.monsterHp = gc.monsterMaxHp / 2 + 5;
            gc.SubmitAnswer(gc.currentQuestion.correctIndex);
            yield return Wait(1.0f);
            yield return Shot("22_enrage");

            gc.StartBattleEncounter("entropy_chaos_king", true);
            yield return Wait(1.2f);
            yield return Shot("23_final_boss");

            // ---- victory ----
            gc.StartBattleEncounter("corrupted_slime", false);
            yield return Wait(1.0f);
            gc.monsterHp = 1;
            gc.ExecuteBasicAttack();
            yield return Wait(2.6f);
            yield return Shot("24_victory");

            // ---- windows ----
            gc.TourSet("guide", 5); yield return Wait(0.3f); yield return Shot("30_guide_defend");
            gc.TourSet("guide", 7); yield return Wait(0.3f); yield return Shot("31_guide_status");
            gc.TourSet("guide", 8); yield return Wait(0.3f); yield return Shot("32_guide_monsters");
            gc.TourSet("codex", 0, 4); yield return Wait(0.3f); yield return Shot("33_codex");
            gc.TourSet("codex", 1, 12); gc.TourSet("bestiaryAnim", 1); yield return Wait(0.5f); yield return Shot("34_bestiary");
            gc.TourSet("shop", 0); yield return Wait(0.3f); yield return Shot("35_shop");
            gc.TourSet("modal", (int)ModalOverlay.TeacherAnalytics); yield return Wait(0.3f); yield return Shot("36_teacher");
            gc.TourSet("modal", (int)ModalOverlay.Settings); yield return Wait(0.3f); yield return Shot("37_settings");

            yield return Wait(0.5f);
            Application.Quit();
        }
    }
}
