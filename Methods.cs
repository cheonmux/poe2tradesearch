using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Poe2TradeSearch
{
    public partial class WinMain : Window
    {
        private void ResetControls()
        {
            tbLinksMin.Text = "";
            tbSocketMin.Text = "";
            tbLinksMax.Text = "";
            tbSocketMax.Text = "";
            tbLvMin.Text = "";
            tbLvMax.Text = "";
            tbQualityMin.Text = "";
            tbQualityMax.Text = "";
            tkDetail.Text = "";

            lbDPS.Content = "옵션";
            Synthesis.Content = "결합";

            cbRarity.Items.Clear();
            cbRarity.Items.Add("모두");
            cbRarity.Items.Add(mParserData.Rarity.Entries[0].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[1].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[2].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[3].Text[0]);

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;
            Synthesis.IsChecked = false;

            cbInfluence1.SelectedIndex = 0;
            cbInfluence2.SelectedIndex = 0;
            cbInfluence1.BorderThickness = new Thickness(1);
            cbInfluence2.BorderThickness = new Thickness(1);

            cbCorrupt.SelectedIndex = 0;
            cbCorrupt.BorderThickness = new Thickness(1);
            cbCorrupt.FontWeight = FontWeights.Normal;
            cbCorrupt.Foreground = cbInfluence1.Foreground;

            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            int exIdx = cbOrbs.Items.IndexOf("엑잘티드 오브");
            cbOrbs.SelectedIndex = exIdx >= 0 ? exIdx : 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;

            cbOrbs.FontWeight = FontWeights.Normal;
            cbSplinters.FontWeight = FontWeights.Normal;

            ckLv.Content = mParserData.Level.Text[0];
            ckLv.FontWeight = FontWeights.Normal;
            ckLv.Foreground = Synthesis.Foreground;
            ckLv.BorderBrush = Synthesis.BorderBrush;
            ckQuality.FontWeight = FontWeights.Normal;
            ckQuality.Foreground = Synthesis.Foreground;
            ckQuality.BorderBrush = Synthesis.BorderBrush;

            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (int)Math.Ceiling(mConfigData.Options.SearchPriceCount / 20) - 1;
            tbPriceFilterMin.Text = mConfigData.Options.SearchPriceMin > 0 ? mConfigData.Options.SearchPriceMin.ToString() : "";

            for (int i = 0; i < 10; i++)
            {
                ((ComboBox)this.FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)this.FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)this.FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValuePath = "Name";

                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = true;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                SetFilterObjectColor(i, SystemColors.ActiveBorderBrush);
            }
        }

        private void SetFilterObjectColor(int index, System.Windows.Media.SolidColorBrush colorBrush)
        {
            ((Control)this.FindName("tbOpt" + index)).BorderBrush = colorBrush;
            ((Control)this.FindName("tbOpt" + index + "_0")).BorderBrush = colorBrush;
            ((Control)this.FindName("tbOpt" + index + "_1")).BorderBrush = colorBrush;
            ((Control)this.FindName("tbOpt" + index + "_2")).BorderBrush = colorBrush;
            ((Control)this.FindName("tbOpt" + index + "_3")).BorderBrush = colorBrush;
        }

        private void SetSearchButtonText(bool is_kor)
        {
            bool isExchange = bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0);
            btnSearch.Content = "거래소에서 " + (isExchange ? "대량 " : "") + "찾기";
        }

        private void setDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
        {
            // DPS 계산 POE-TradeMacro 참고

            double physicalDPS = DamageToDPS(physical);
            double elementalDPS = DamageToDPS(elemental);
            double chaosDPS = DamageToDPS(chaos);

            double quality20Dps = quality == "" ? 0 : StrToDouble(quality, 0);
            double attacksPerSecond = StrToDouble(Regex.Replace(perSecond, "[^0-9.]", ""), 0);

            if (speedIncr > 0)
            {
                double baseAttackSpeed = attacksPerSecond / (speedIncr / 100 + 1);
                double modVal = baseAttackSpeed % 0.05;
                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                attacksPerSecond = baseAttackSpeed * (speedIncr / 100 + 1);
            }

            physicalDPS = (physicalDPS / 2) * attacksPerSecond;
            elementalDPS = (elementalDPS / 2) * attacksPerSecond;
            chaosDPS = (chaosDPS / 2) * attacksPerSecond;

            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
            quality20Dps = quality20Dps < 20 ? physicalDPS * (phyDmgIncr + 120) / (phyDmgIncr + quality20Dps + 100) : 0;
            physicalDPS = quality20Dps > 0 ? quality20Dps : physicalDPS;

            lbDPS.Content = "DPS: P." + Math.Round(physicalDPS, 2).ToString() +
                            " + E." + Math.Round(elementalDPS, 2).ToString() +
                            " = T." + Math.Round(physicalDPS + elementalDPS + chaosDPS, 2).ToString();
        }

        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            string item_category = "";
            string item_name = "";
            string item_type = "";
            string item_rarity = "";
            ParserData PS = mParserData;

            try
            {
                string itemTextNorm = (itemText ?? "").Replace("\r\n", "\n");
                string[] asData = itemTextNorm.Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && (asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 || asData[0].IndexOf(PS.Category.Text[1] + ": ") == 0))
                {
                    byte z = (byte)(asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 ? 0 : 1);
                    // 서버는 항상 한국(0) 고정

                    ResetControls();
                    mItemBaseName = new ItemBaseName();
                    mItemBaseName.LangType = z;

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                    item_category = asOpt[0].Split(':')[1].Trim();
                    item_rarity = asOpt[1].Split(':')[1].Trim();

                    item_name = Regex.Replace(asOpt[2] ?? "", @"<<set:[A-Z]+>>", "");
                    if (asOpt.Length > 3 && asOpt[3] != "")
                    {
                        item_type = Regex.Replace(asOpt[3] ?? "", @"<<set:[A-Z]+>>", "");
                    }
                    else
                    {
                        item_type = item_name;
                        item_name = "";
                    }

                    ParserDictionary rarity = Array.Find(PS.Rarity.Entries, x => x.Text[z] == item_rarity);
                    string rarity_id = rarity != null ? rarity.Id : "";

                    ParserDictionary category = Array.Find(PS.Category.Entries, x => x.Text[z] == item_type)
                                            ?? Array.Find(PS.Category.Entries, x => x.Text[z] == item_name);

                    // 마법(magic) 아이템은 이름이 "접두 + 베이스 + 접미"라 풀네임으론 category 매칭 실패.
                    // 접미(" - "/" of ")를 떼고 접두 토큰을 앞에서 하나씩 벗기며 베이스로 재매칭한다.
                    if (category == null && rarity_id == "magic")
                    {
                        string baseName = item_type.Split(new string[] { z == 1 ? " of " : " - " }, StringSplitOptions.None)[0].Trim();
                        string[] toks = baseName.Split(' ');
                        for (int i = 0; i < toks.Length && category == null; i++)
                        {
                            string cand = string.Join(" ", toks, i, toks.Length - i);
                            category = Array.Find(PS.Category.Entries, x => x.Text[z] == cand);
                        }
                    }

                    string[] cate_ids = category != null ? category.Id.Split('.') : new string[] { "" };

                    item_rarity = rarity != null ? rarity.Text[0] : item_rarity;

                    int k = 0, baki = 0;
                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;
                    bool is_prophecy = false;
                    int prefixCount = 0, suffixCount = 0;
                    string currentSectionType = "explicit"; // 현재 { } 헤더 기반 섹션 타입

                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { PS.Quality.Text[z], "" }, { PS.Level.Text[z], "" }, { PS.ItemLevel.Text[z], "" }, { PS.TalismanTier.Text[z], "" }, { PS.MapTier.Text[z], "" },
                        { PS.Sockets.Text[z], "" }, { PS.PhysicalDamage.Text[z], "" }, { PS.ElementalDamage.Text[z], "" }, { PS.ChaosDamage.Text[z], "" },
                        { PS.AttacksPerSecond.Text[z], "" }, { PS.ShaperItem.Text[z], "" }, { PS.ElderItem.Text[z], "" }, { PS.CrusaderItem.Text[z], "" },
                        { PS.RedeemerItem.Text[z], "" }, { PS.HunterItem.Text[z], "" }, { PS.WarlordItem.Text[z], "" }, { PS.SynthesisedItem.Text[z], "" },
                        { PS.Corrupted.Text[z], "" }, { PS.Unidentified.Text[z], "" }, { PS.MonsterGenus.Text[z], "" }, { PS.MonsterGroup.Text[z], "" },
                        { PS.Vaal.Text[z] + " " + item_type, "" }
                    };

                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                        // POE2: 같은 섹션에 메타정보+옵션이 함께 있을 수 있으므로 먼저 전체 스캔
                        foreach (string line in asOpt)
                        {
                            if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^\{.*접두어")) prefixCount++;
                            else if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^\{.*접미어")) suffixCount++;
                            if (line.Trim() == "" || line.Trim().StartsWith("{")) continue;
                            if (line.IndexOf("(rune)") > -1) continue;
                            string[] preTmp = Regex.Replace(line, @" \([\w\s]+\)\: ", ": ").Split(':');
                            if (lItemOption.ContainsKey(preTmp[0]) && lItemOption[preTmp[0]] == "")
                                lItemOption[preTmp[0]] = preTmp.Length > 1 ? preTmp[1].Trim() : "_TRUE_";
                        }

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            if (asOpt[j].Trim() == "") continue;
                            if (asOpt[j].Trim().StartsWith("{"))
                            {
                                string hdr = asOpt[j].Trim();
                                if (hdr == "{ 향상 }") currentSectionType = "enchant";
                                else if (hdr.StartsWith("{ 고정 속성 부여")) currentSectionType = "implicit";
                                else currentSectionType = "explicit"; // 고유/접두어/접미어 등 나머지는 모두 explicit
                                continue;
                            }
                            if (asOpt[j].IndexOf("(rune)") > -1) continue;

                            string optLine = Regex.Replace(asOpt[j], @"\s*—\s*변경이 불가능한 값$", "");
                            string[] asTmp = Regex.Replace(optLine, @" \([\w\s]+\)\: ", ": ").Split(':');

                            if (lItemOption.ContainsKey(asTmp[0]))
                            {
                                if (lItemOption[asTmp[0]] == "")
                                    lItemOption[asTmp[0]] = asTmp.Length > 1 ? asTmp[1].Trim() : "_TRUE_";
                            }
                            else
                            {
                                if (!is_prophecy && PS.ProphecyItem?.Text != null && asTmp[0].IndexOf(PS.ProphecyItem.Text[z]) == 0)
                                    is_prophecy = true;
                                else if (lItemOption[PS.ItemLevel.Text[z]] != "" && k < 10)
                                {
                                    double min = 99999, max = 99999;
                                    bool resistance = false;
                                    bool crafted = optLine.IndexOf("(crafted)") > -1;

                                    string input = Regex.Replace(optLine, @" \([a-zA-Z]+\)", "");
                                    input = Regex.Replace(input, @"\([+-]?[0-9]+\.?[0-9]*-[+-]?[0-9]+\.?[0-9]*\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");
                                    //input = input + (is_captured_beast ? "\\(" + RS.Captured[z] + "\\)" : "");

                                    bool local_exists = false;
                                    DataEntrie filter = null;
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);

                                    foreach (DataResult data_result in mFilterData[z].Result)
                                    {
                                        DataEntrie[] entries = Array.FindAll(data_result.Entries, x => rgx.IsMatch(x.Text));

                                        // 2개 이상 같은 옵션이 있을때 장비 옵션 (특정) 만 추출
                                        if (entries.Length > 1)
                                        {
                                            // POE2: part 필드 없음 → lParticular + 카테고리로 로컬 stat 판별
                                            DataEntrie[] entries_tmp = Array.FindAll(entries, x => {
                                                string[] idParts = x.Id.Split('.');
                                                if (idParts.Length != 2 || !RS.lParticular.ContainsKey(idParts[1])) return false;
                                                byte partVal = RS.lParticular[idParts[1]];
                                                return (partVal == 1 && cate_ids[0] == "weapon") || (partVal == 2 && cate_ids[0] != "weapon");
                                            });
                                            if (entries_tmp.Length > 0)
                                            {
                                                local_exists = true;
                                                entries = entries_tmp;
                                            }
                                        }
                                        else if (entries.Length == 1)
                                        {
                                            // 1개만 매칭됐을 때: 글로벌 버전이고 같은 그룹에 (특정) 버전이 있으면 카테고리에 맞게 교체
                                            string[] matchedIdParts = entries[0].Id.Split('.');
                                            if (matchedIdParts.Length == 2 && !RS.lParticular.ContainsKey(matchedIdParts[1]))
                                            {
                                                string localText = entries[0].Text + "(특정)";
                                                DataEntrie[] entries_tmp = Array.FindAll(data_result.Entries, x => {
                                                    string[] idParts = x.Id.Split('.');
                                                    if (x.Text != localText || idParts.Length != 2 || !RS.lParticular.ContainsKey(idParts[1])) return false;
                                                    byte partVal = RS.lParticular[idParts[1]];
                                                    return (partVal == 1 && cate_ids[0] == "weapon") || (partVal == 2 && cate_ids[0] != "weapon");
                                                });
                                                if (entries_tmp.Length > 0)
                                                {
                                                    local_exists = true;
                                                    entries = entries_tmp;
                                                }
                                            }
                                        }

                                        if (entries.Length > 0)
                                        {
                                            Array.Sort(entries, delegate (DataEntrie entrie1, DataEntrie entrie2)
                                            {
                                                return (entrie2.Part ?? "").CompareTo(entrie1.Part ?? "");
                                            });

                                            MatchCollection matches1 = Regex.Matches(optLine, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                            foreach (DataEntrie entrie in entries)
                                            {
                                                int idxMin = 0, idxMax = 0;
                                                bool isMin = false, isMax = false;
                                                bool isBreak = true;

                                                MatchCollection matches2 = Regex.Matches(entrie.Text, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+|#");

                                                for (int t = 0; t < matches2.Count; t++)
                                                {
                                                    if (matches2[t].Value == "#")
                                                    {
                                                        if (!isMin)
                                                        {
                                                            isMin = true;
                                                            idxMin = t;
                                                        }
                                                        else if (!isMax)
                                                        {
                                                            isMax = true;
                                                            idxMax = t;
                                                        }
                                                    }
                                                    else if (t >= matches1.Count || matches1[t].Value != matches2[t].Value)
                                                    {
                                                        isBreak = false;
                                                        break;
                                                    }
                                                }

                                                if (isBreak)
                                                {
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.Id, data_result.Label));

                                                    if (filter == null)
                                                    {
                                                        string[] id_split = entrie.Id.Split('.');
                                                        resistance = id_split.Length == 2 && RS.lResistance.ContainsKey(id_split[1]);
                                                        filter = entrie;

                                                        MatchCollection matches = Regex.Matches(optLine, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                                        min = isMin && matches.Count > idxMin ? StrToDouble(((Match)matches[idxMin]).Value, 99999) : 99999;
                                                        max = isMax && idxMin < idxMax && matches.Count > idxMax ? StrToDouble(((Match)matches[idxMax]).Value, 99999) : 99999;
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (filter != null)
                                    {
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["crafted"];
                                        int selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                        if (crafted && selidx > -1)
                                        {
                                            SetFilterObjectColor(k, System.Windows.Media.Brushes.Blue);
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }
                                        else
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["pseudo"];
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count > 0)
                                            {
                                                FilterEntrie filterEntrie = (FilterEntrie)((ComboBox)this.FindName("cbOpt" + k)).Items[0];
                                                string[] id_split = filterEntrie.ID.Split('.');
                                                if (id_split.Length == 2 && RS.lPseudo.ContainsKey(id_split[1]))
                                                {
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie("pseudo." + RS.lPseudo[id_split[1]], RS.lFilterType["pseudo"]));
                                                }
                                            }

                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).Items.Count == 1 ? 0 : -1;

                                            //if (is_captured_beast)
                                            //{
                                            //    ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["monster"];
                                            //    selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            //}
                                            //else

                                            // 인첸트, 제작은 다른 곳에서 다시 체크함
                                            string[] tmps = { !local_exists && mConfigData.Options.AutoSelectPseudo ? "pseudo" : "explicit", "explicit", "fractured" };
                                            foreach (string tmp in tmps)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType[tmp];
                                                if (((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex > -1)
                                                {
                                                    selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                                    break;
                                                }
                                            }

                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }

                                        if (i != baki)
                                        {
                                            baki = i;
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k)).Text = filter.Text;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_3")).Visibility = resistance ? Visibility.Visible : Visibility.Hidden;
                                        if (((CheckBox)this.FindName("tbOpt" + k + "_3")).Visibility == Visibility.Visible && mConfigData.Options.AutoCheckTotalres)
                                            ((CheckBox)this.FindName("tbOpt" + k + "_3")).IsChecked = true;

                                        if (min != 99999 && max != 99999)
                                        {
                                            if (filter.Text.IndexOf("#~#") > -1)
                                            {
                                                min += max;
                                                min = Math.Truncate(min / 2 * 10) / 10;
                                                max = 99999;
                                            }
                                        }
                                        else if (min != 99999 || max != 99999)
                                        {
                                            string[] split = filter.Id.Split('.');
                                            bool defMaxPosition = split.Length == 2 && RS.lDefaultPosition.ContainsKey(split[1]);
                                            if ((defMaxPosition && min > 0 && max == 99999) || (!defMaxPosition && min < 0 && max == 99999))
                                            {
                                                max = min;
                                                min = 99999;
                                            }
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();

                                        Itemfilter itemfilter = new Itemfilter
                                        {
                                            id = currentSectionType,
                                            text = filter.Text,
                                            max = max,
                                            min = min,
                                            disabled = true
                                        };

                                        itemfilters.Add(itemfilter);

                                        if (filter.Text == PS.AttackSpeedIncr.Text[z] && min > 0 && min < 999)
                                        {
                                            attackSpeedIncr += min;
                                        }
                                        else if (filter.Text == PS.PhysicalDamageIncr.Text[z] && min > 0 && min < 9999)
                                        {
                                            PhysicalDamageIncr += min;
                                        }

                                        k++;
                                    }
                                }
                            }
                        }
                    }

                    // 희귀 아이템: 빈 접두어/접미어 슬롯 자동 추가 (최대 3개씩, 지도 제외)
                    if (rarity_id == "rare" && cate_ids[0] != "map" && k < 10)
                    {
                        int emptyPrefix = Math.Max(0, 3 - prefixCount);
                        int emptySuffix = Math.Max(0, 3 - suffixCount);

                        string[] emptyStats = {
                            "pseudo.pseudo_number_of_empty_prefix_mods",
                            "pseudo.pseudo_number_of_empty_suffix_mods"
                        };
                        int[] emptyCounts = { emptyPrefix, emptySuffix };
                        string[] emptyLabels = { "# 빈 접두어 속성 부여", "# 빈 접미어 속성 부여" };

                        for (int e = 0; e < 2 && k < 10; e++)
                        {
                            if (emptyCounts[e] > 0)
                            {
                                string pseudoLabel = RS.lFilterType.ContainsKey("pseudo") ? RS.lFilterType["pseudo"] : "유사";
                                ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(emptyStats[e], pseudoLabel));
                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = 0;
                                ((TextBox)this.FindName("tbOpt" + k)).Text = emptyLabels[e];
                                ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = emptyCounts[e].ToString();
                                ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = "";
                                k++;
                            }
                        }
                    }

                    int alt_quality = 0;
                    bool is_blight = false;

                    /*
                    //if (is_map || is_currency) is_map_fragment = false;
                    */
                    bool is_map = cate_ids[0] == "map"; // || lItemOption[PS.MapTier.Text[z]] != "";
                    bool is_currency = rarity_id == "currency";
                    bool is_divinationCard = rarity_id == "card";
                    bool is_gem = rarity_id == "gem";
                    // 보조 젬(POE2 신규): "아이템 종류: 보조 젬". trade에 안 잡히고 poe.ninja(LineageSupportGems)에 시세 존재.
                    // → 이름(item_name)으로 ninja/static 매칭되면 화폐처럼 ninja 단일 시세 표시.
                    bool is_supportgem = is_gem && item_category == "보조 젬" && GetExchangeItem(z, item_name) != null;
                    bool is_vaal_gem = is_gem && lItemOption[PS.Vaal.Text[z] + " " + item_type] == "_TRUE_";
                    bool is_detail = is_gem || is_currency || is_divinationCard || is_prophecy;
                    bool is_unIdentify = lItemOption[PS.Unidentified.Text[z]] == "_TRUE_";

                    if (lItemOption.ContainsKey(PS.Sockets.Text[z]) && lItemOption[PS.Sockets.Text[z]] != "")
                    {
                        // POE2: "홈: S S" 형식 — S 개수 = 소켓 수
                        string socket = lItemOption[PS.Sockets.Text[z]];
                        int sckcnt = Regex.Matches(socket, @"\bS\b").Count;
                        if (sckcnt == 0) sckcnt = socket.Trim().Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries).Length;
                        tbSocketMin.Text = sckcnt.ToString();
                        tbLinksMin.Text = "";
                        // 홈은 자동 체크하지 않음 (개수만 칸에 채우고, 검색 포함은 사용자가 수동 선택).
                        ckSocket.IsChecked = false;
                    }

                    int item_idx = -1;
                    int cate_idx = category != null ? Array.FindIndex(mItemsData[z].Result, x => x.Id.Equals(category.Key)) : -1;

                    if (is_prophecy)
                    {
                        cate_ids = new string[] { "prophecy" };
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Key == "prophecies").Text[z];
                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => x.Type == item_type);
                    }
                    else if (lItemOption[PS.MonsterGenus.Text[z]] != "" && lItemOption[PS.MonsterGroup.Text[z]] != "")
                    {
                        cate_ids = new string[] { "monster", "beast" };
                        cate_idx = Array.FindIndex(mItemsData[z].Result, x => x.Id.Equals("monsters"));
                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => x.Text == item_type);
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Id == "monster.beast").Text[z];
                        item_type = z == 1 || item_idx == -1 ? item_type : mItemsData[1].Result[cate_idx].Entries[item_idx].Type;
                        item_idx = -1; // 야수는 영어로만 검색됨...
                    }
                    else if(cate_idx > -1)
                    {
                        DataResult data = mItemsData[z].Result[cate_idx];

                        if ((is_unIdentify || rarity_id == "normal") && item_type.Length > 4 && item_type.IndexOf(PS.Superior.Text[z] + " ") == 0)
                        {
                            item_type = item_type.Substring(z == 1 ? 9 : 3);
                        }
                        else if (rarity_id == "magic")
                        {
                            item_type = item_type.Split(new string[] { z == 1 ? " of " : " - " }, StringSplitOptions.None)[0].Trim();
                        }

                        if (is_gem)
                        {
                            for (int i = 0; i < PS.Gems.Entries.Length; i++)
                            {
                                int pos = item_type.IndexOf(PS.Gems.Entries[i].Text[z] + " ");
                                if (pos == 0)
                                {
                                    alt_quality = i + 1;
                                    item_type = item_type.Substring(PS.Gems.Entries[i].Text[z].Length + 1);
                                }
                            }

                            if (is_vaal_gem && lItemOption[PS.Corrupted.Text[z]] == "_TRUE_")
                            {
                                DataEntrie entries = Array.Find(data.Entries, x => x.Text.Equals(PS.Vaal.Text[z] + " " + item_type));
                                if (entries != null) item_type = entries.Type;
                            }
                        }
                        else if (is_map && item_type.Length > 5)
                        {
                            if (item_type.IndexOf(PS.Blighted.Text[z] + " ") == 0)
                            {
                                is_blight = true;
                                item_type = item_type.Substring(z == 1 ? 9 : 6);
                            }

                            if (item_type.Substring(0, z == 1 ? 7 : 4) == PS.Shaped.Text[z] + " ")
                                item_type = item_type.Substring(z == 1 ? 7 : 4);
                        }
                        else if (lItemOption[PS.SynthesisedItem.Text[z]] == "_TRUE_")
                        {
                            if (item_type.Substring(0, z == 1 ? 12 : 4) == PS.Synthesised.Text[z] + " ")
                                item_type = item_type.Substring(z == 1 ? 12 : 4);
                        }

                        if (!is_unIdentify && rarity_id == "magic")
                        {
                            string[] tmp = item_type.Split(' ');

                            if (data != null && tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = string.Join(" ", tmp).Trim();

                                    DataEntrie entries = Array.Find(data.Entries, x => x.Type.Equals(tmp2));
                                    if (entries != null)
                                    {
                                        item_type = entries.Type;
                                        break;
                                    }
                                }
                            }
                        }

                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => (x.Type == item_type && (rarity_id != "unique" || x.Name == item_name)));
                    }


                    mItemBaseName.Ids = cate_ids;
                    mItemBaseName.NameEN = z == 1 || cate_idx == -1 || item_idx == -1 || rarity_id != "unique" ? item_name : mItemsData[1].Result[cate_idx].Entries[item_idx].Name;
                    mItemBaseName.NameKR = z == 0 || cate_idx == -1 || item_idx == -1 || rarity_id != "unique" ? item_name : mItemsData[0].Result[cate_idx].Entries[item_idx].Name;
                    mItemBaseName.TypeEN = z == 1 || cate_idx == -1 || item_idx == -1 ? item_type : mItemsData[1].Result[cate_idx].Entries[item_idx].Type;
                    mItemBaseName.TypeKR = z == 0 || cate_idx == -1 || item_idx == -1 ? item_type : mItemsData[0].Result[cate_idx].Entries[item_idx].Type;


                    string item_quality = Regex.Replace(lItemOption[PS.Quality.Text[z]], "[^0-9]", "");
                    bool by_type = cate_ids.Length > 1 && (cate_ids[0] == "weapon" || cate_ids[0] == "armour" || cate_ids[0] == "accessory");

                    bool is_fragment = cate_ids.Length > 1 && cate_ids[1] == "fragment";

                    if (is_detail || is_fragment)
                    {
                        try
                        {
                            int i = is_fragment ? 1 : (is_gem ? 3 : 2);
                            tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                            tkDetail.Text = Regex.Replace(
                                tkDetail.Text.Replace(PS.UnstackItems.Text[z], ""),
                                "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>",
                                ""
                            );
                        }
                        catch { }
                    }
                    else
                    {
                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            Itemfilter ifilter = itemfilters[i];
                            bool isNonExplicit = ifilter.id == "implicit" || ifilter.id == "enchant";

                            if (isNonExplicit)
                            {
                                SetFilterObjectColor(i, System.Windows.Media.Brushes.Blue);
                                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = RS.lFilterType[ifilter.id];
                                if (((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex == -1)
                                {
                                    SetFilterObjectColor(i, System.Windows.Media.Brushes.DarkRed);
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = RS.lFilterType["implicit"];
                                }

                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                itemfilters[i].disabled = true;
                            }
                            else if (cate_ids[0] != "" && ((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex > -1)
                            {
                                if ((string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue != RS.lFilterType["crafted"]
                                    && ((mConfigData.Options.AutoCheckUnique && rarity_id == "unique")
                                    || (Array.Find(mParserData.Checked.Entries, x => x.Text[z] == ifilter.text && x.Id.IndexOf(cate_ids[0] + "/") > -1) != null)))
                                {
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                    itemfilters[i].disabled = false;
                                }
                            }
                        }

                        // 장기는 중복 옵션 제거
                        if (cate_ids.Length > 1 && cate_ids[0] == "monster" && cate_ids[1] == "sample")
                        {
                            for (int i = 0; i < itemfilters.Count; i++)
                            {
                                string txt = ((TextBox)this.FindName("tbOpt" + i)).Text;
                                if (((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled == false) continue;
                                for (int j = 0; j < itemfilters.Count; j++)
                                {
                                    if (i == j) continue;
                                    if (((TextBox)this.FindName("tbOpt" + j)).Text == txt)
                                    {
                                        ((CheckBox)this.FindName("tbOpt" + j + "_2")).IsChecked = false;
                                        ((CheckBox)this.FindName("tbOpt" + j + "_2")).IsEnabled = false;
                                        itemfilters[j].disabled = true;
                                    }
                                }
                            }
                        }

                        if (!is_unIdentify && cate_ids[0] == "weapon")
                        {
                            setDPS(
                                    lItemOption[PS.PhysicalDamage.Text[z]], lItemOption[PS.ElementalDamage.Text[z]], lItemOption[PS.ChaosDamage.Text[z]],
                                    item_quality, lItemOption[PS.AttacksPerSecond.Text[z]], PhysicalDamageIncr, attackSpeedIncr
                                );
                        }
                    }

                    cbName.SelectionChanged -= cbName_SelectionChanged;
                    cbName.Items.Clear();
                    cbName.Items.Add((Regex.Replace(mItemBaseName.NameKR, @"\([a-zA-Z\,\s']+\)$", "") + " " + Regex.Replace(mItemBaseName.TypeKR, @"\([a-zA-Z\,\s']+\)$", "")).Trim());
                    cbName.Items.Add("아이템 유형으로 검색합니다");
                    cbName.SelectedIndex = 0;
                    if (by_type && (rarity_id == "magic" || rarity_id == "rare"))
                    {
                        string[] bys = mConfigData.Options.AutoSelectByType.ToLower().Split(',');
                        if (Array.IndexOf(bys, cate_ids[0]) > -1) cbName.SelectedIndex = 2;
                    }
                    cbName.SelectionChanged += cbName_SelectionChanged;

                    cbRarity.SelectedValue = item_rarity;
                    if (cbRarity.SelectedIndex == -1)
                    {
                        cbRarity.Items.Clear();
                        cbRarity.Items.Add(item_rarity);
                        cbRarity.SelectedIndex = 0;
                    }
                    else if ((string)cbRarity.SelectedValue == "normal")
                    {
                        cbRarity.SelectedIndex = 0;
                    }

                    bool Is_exchangeCurrency = (cate_ids[0] == "currency" && GetExchangeItem(z, item_type) != null) || is_supportgem;
                    // 보조 젬도 ninja 시세 대상 → exchange UI 표시/활성 (일반 젬은 제외)
                    bdExchange.Visibility = (!is_gem || is_supportgem) && (is_detail || Is_exchangeCurrency) ? Visibility.Visible : Visibility.Hidden;
                    bdExchange.IsEnabled = Is_exchangeCurrency;

                    if (bdExchange.Visibility == Visibility.Hidden)
                    {
                        tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? PS.Level.Text[z] : PS.ItemLevel.Text[z]], "[^0-9]", "");
                        tbQualityMin.Text = item_quality;

                        string[] Influences = { PS.ShaperItem.Text[z], PS.ElderItem.Text[z], PS.CrusaderItem.Text[z], PS.RedeemerItem.Text[z], PS.HunterItem.Text[z], PS.WarlordItem.Text[z] };
                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence1.SelectedIndex = i + 1;
                        }

                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (cbInfluence1.SelectedIndex != (i + 1) && lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence2.SelectedIndex = i + 1;
                        }

                        if (lItemOption[PS.Corrupted.Text[z]] == "_TRUE_")
                        {
                            cbCorrupt.BorderThickness = new Thickness(2);
                            cbCorrupt.FontWeight = FontWeights.Bold;
                            cbCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                        }
                        else
                        {
                            cbCorrupt.SelectedIndex = 2; // 타락 없음 → "아니오" 자동 설정
                        }

                        Synthesis.IsChecked = (is_map && is_blight) || lItemOption[PS.SynthesisedItem.Text[z]] == "_TRUE_";

                        if (cbInfluence1.SelectedIndex > 0) cbInfluence1.BorderThickness = new Thickness(2);
                        if (cbInfluence2.SelectedIndex > 0) cbInfluence2.BorderThickness = new Thickness(2);

                        if (is_map)
                        {
                            tbLvMin.Text = tbLvMax.Text = lItemOption[PS.MapTier.Text[z]];
                            ckLv.Content = "등급";
                            ckLv.IsChecked = true;
                            Synthesis.Content = "역병";
                        }
                        else if (is_gem)
                        {
                            ckLv.IsChecked = lItemOption[PS.Level.Text[z]].IndexOf(" (" + PS.Max.Text[z]) > 0;
                            ckQuality.IsChecked = ckLv.IsChecked == true && item_quality != "" && int.Parse(item_quality) > 19;
                        }
                        else if (by_type || cate_ids[0] == "flask")
                        {
                            if (tbQualityMin.Text != "" && int.Parse(tbQualityMin.Text) > (cate_ids[0] == "accessory" ? 4 : 20))
                            {
                                ckQuality.FontWeight = FontWeights.Bold;
                                ckQuality.Foreground = System.Windows.Media.Brushes.DarkRed;
                                ckQuality.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            }

                            if (by_type)
                            {
                                if (tbLvMin.Text != "" && int.Parse(tbLvMin.Text) > 82)
                                {
                                    ckLv.FontWeight = FontWeights.Bold;
                                    ckLv.Foreground = System.Windows.Media.Brushes.DarkRed;
                                    ckLv.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                }

                                cbCorrupt.SelectedIndex = mConfigData.Options.AutoSelectCorrupt == "no" ? 2 : (mConfigData.Options.AutoSelectCorrupt == "yes" ? 1 : 0);
                            }
                        }
                    }

                    bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;

                    if (isWinShow || this.Visibility == Visibility.Visible)
                    {
                        tkPriceInfo.Foreground = tkPriceCount.Foreground = SystemColors.WindowTextBrush;

                        mLockUpdatePrice = false;

                        // 새 아이템 Ctrl+C → 이전 자동검색 타이머를 멈추고 무조건 새로 검색한다.
                        // (rate limit은 UpdatePrice 내 WaitForRateLimit가 보내기 전에 강제하므로 안전)
                        mAutoSearchTimer.Stop();
                        mAutoSearchTimerCount = 0;

                        if (mConfigData.Options.AutoSearchDelay > 0)
                        {
                            if (bdExchange.Visibility == Visibility.Visible && cbOrbs.SelectedIndex >= 0 && mItemBaseName != null)
                            {
                                // 보조 젬은 이름(NameKR)으로 ninja 매칭, 일반 화폐는 TypeKR. NameKR 우선 폴백.
                                ParserDictionary ei1 = (!string.IsNullOrEmpty(mItemBaseName.NameKR) ? GetExchangeItem(0, mItemBaseName.NameKR) : null)
                                                       ?? GetExchangeItem(0, mItemBaseName.TypeKR);
                                ParserDictionary ei2 = GetExchangeItem(0, (string)cbOrbs.SelectedValue);
                                if (ei1 != null && ei2 != null)
                                    UpdatePriceThreadWorker(null, new string[] { ei1.Id, ei2.Id });
                                else
                                    UpdatePriceThreadWorker(GetItemOptions(), null);
                            }
                            else
                            {
                                UpdatePriceThreadWorker(GetItemOptions(), null);
                            }
                        }
                        else
                        {
                            liPrice.Items.Clear();
                            tkPriceCount.Text = "";
                            cbPriceListTotal.Text = "0/0 검색";
                            //tkPriceInfo.Foreground = tkPriceCount.Foreground = System.Windows.SystemColors.HighlightBrush;
                        }

                        SetSearchButtonText(RS.ServerLang == 0);
                        this.ShowActivated = false;
                        this.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                string logPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                logPath = logPath.Remove(logPath.Length - 4) + ".log";
                Exception cur = ex;
                while (cur != null)
                {
                    System.IO.File.AppendAllText(logPath, string.Format("[PARSER] {0}: {1}\r\n{2}\r\n\r\n", cur.GetType().Name, cur.Message, cur.StackTrace));
                    cur = cur.InnerException;
                }
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n", ex.Source, ex.Message), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.Corrupt = (byte)cbCorrupt.SelectedIndex;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.SelectedIndex == 2;

            itemOption.SocketMin = StrToDouble(tbSocketMin.Text, 99999);
            itemOption.SocketMax = StrToDouble(tbSocketMax.Text, 99999);
            itemOption.LinkMin = StrToDouble(tbLinksMin.Text, 99999);
            itemOption.LinkMax = StrToDouble(tbLinksMax.Text, 99999);
            itemOption.QualityMin = StrToDouble(tbQualityMin.Text, 99999);
            itemOption.QualityMax = StrToDouble(tbQualityMax.Text, 99999);
            itemOption.LvMin = StrToDouble(tbLvMin.Text, 99999);
            itemOption.LvMax = StrToDouble(tbLvMax.Text, 99999);

            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : StrToDouble(tbPriceFilterMin.Text, 99999);
            itemOption.RarityAt = (byte)(cbRarity.Items.Count > 1 ? cbRarity.SelectedIndex : 0);

            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)this.FindName("cbOpt" + i);

                if (comboBox.SelectedIndex > -1)
                {
                    itemfilter.text = ((TextBox)this.FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.disabled = ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_0")).Text, 99999);
                    itemfilter.max = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_1")).Text, 99999);

                    if (itemfilter.disabled == false && ((CheckBox)this.FindName("tbOpt" + i + "_3")).IsChecked == true)
                    {
                        if (total_res_idx == -1)
                            total_res_idx = itemOption.itemfilters.Count;
                        else
                        {
                            itemOption.itemfilters[total_res_idx].min += itemfilter.min == 99999 ? 0 : itemfilter.min;
                            itemOption.itemfilters[total_res_idx].max += itemfilter.max == 99999 ? 0 : itemfilter.max;
                            continue;
                        }

                        itemfilter.id = "pseudo.pseudo_total_resistance";
                    }
                    else
                    {
                        itemfilter.id = ((FilterEntrie)comboBox.SelectedItem).ID;
                    }

                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            return itemOption;
        }

        private string CreateJson(ItemOption itemOptions, bool useSaleType)
        {
            string BeforeDayToString(int day)
            {
                if (day < 1)
                    return "any";
                else if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            try
            {
                JsonData jsonData = new JsonData();
                jsonData.Query = new q_Query();
                q_Query JQ = jsonData.Query;

                jsonData.Sort.Price = "asc";

                if (mItemBaseName == null || mItemBaseName.Ids == null) return "";
                byte lang_type = mItemBaseName.LangType;
                string Inherit = mItemBaseName.Ids.Length > 0 ? mItemBaseName.Ids[0] : "any";

                JQ.Name = mItemBaseName.NameKR;
                JQ.Type = mItemBaseName.TypeKR;

                JQ.Stats = new q_Stats[1] { new q_Stats { Type = "and", Filters = new q_Stats_filters[0] } };
                JQ.Status.Option = "securable";

                string categoryOption = Inherit == "jewel" ? Inherit : string.Join(".", mItemBaseName.Ids);
                JQ.Filters.Type.Filters.Category = string.IsNullOrEmpty(categoryOption) ? null : new q_Option { Option = categoryOption };
                JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? (mParserData.Rarity.Entries[itemOptions.RarityAt - 1].Id) : "any";
                //JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? RS.lRarity.ElementAt(itemOptions.RarityAt - 1).Key.ToLower() : "any";

                JQ.Filters.Trade.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                JQ.Filters.Trade.Filters.Indexed.Option = BeforeDayToString(mConfigData.Options.SearchBeforeDay);
                JQ.Filters.Trade.Filters.Price.Min = 99999;
                JQ.Filters.Trade.Filters.Price.Max = 99999;

                if (itemOptions.PriceMin > 0)
                {
                    JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin;
                }

                // equipment_filters: 홈(rune_sockets) 체크시에만 포함
                if (itemOptions.ChkSocket == true)
                {
                    JQ.Filters.Equipment = new q_Equipment_filters();
                    JQ.Filters.Equipment.Filters.RuneSockets.Min = itemOptions.SocketMin;
                    JQ.Filters.Equipment.Filters.RuneSockets.Max = itemOptions.SocketMax;
                }

                // misc_filters: 품질/부패/레벨 체크시에만 포함
                bool useMisc = itemOptions.ChkQuality == true || itemOptions.Corrupt != 0
                    || (Inherit != "map" && itemOptions.ChkLv == true);
                if (useMisc)
                {
                    JQ.Filters.Misc = new q_Misc_filters();
                    JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                    JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;
                    JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv == true && Inherit != "gem" && Inherit != "map" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv == true && Inherit != "gem" && Inherit != "map" ? itemOptions.LvMax : 99999;
                    JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMin : 99999;
                    JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMax : 99999;
                    JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");
                }

                // map_filters: 맵 티어 체크시에만 포함
                if (Inherit == "map" && itemOptions.ChkLv == true)
                {
                    JQ.Filters.Map = new q_Map_filters();
                    JQ.Filters.Map.Filters.Tier.Min = itemOptions.LvMin;
                    JQ.Filters.Map.Filters.Tier.Max = itemOptions.LvMax;
                }

                bool error_filter = false;

                if (itemOptions.itemfilters.Count > 0)
                {
                    JQ.Stats = new q_Stats[1];
                    JQ.Stats[0] = new q_Stats();
                    JQ.Stats[0].Type = "and";
                    JQ.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

                    int idx = 0;

                    for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                    {
                        string input = itemOptions.itemfilters[i].text;
                        string id = itemOptions.itemfilters[i].id;
                        string type = itemOptions.itemfilters[i].id.Split('.')[0];

                        if (input.Trim() != "")
                        {
                            string type_name = RS.lFilterType[type];

                            DataResult filterResult = Array.Find(mFilterData[lang_type].Result, x => x.Label == type_name);

                            // 무기에 경우 pseudo_adds_[a-z]+_damage 옵션은 공격 시 가 붙음
                            if (type == "pseudo" && Inherit == "weapon" && Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                            {
                                id = id + "_to_attacks";
                            }

                            if (filterResult == null)
                            {
                                continue;
                            }

                            DataEntrie filter = Array.Find(filterResult.Entries, x => x.Id == id && x.Type == type);

                            JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                            JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();

                            if (filter != null && (filter.Id ?? "").Trim() != "")
                            {
                                JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                JQ.Stats[0].Filters[idx++].Id = filter.Id;
                            }
                            else
                            {
                                error_filter = true;
                                itemOptions.itemfilters[i].isNull = true;

                                // 오류 방지를 위해 널값시 아무거나 추가
                                JQ.Stats[0].Filters[idx].Disabled = true;
                                JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                JQ.Stats[0].Filters[idx].Value.Max = 99999;
                                JQ.Stats[0].Filters[idx++].Id = "temp_ids";
                            }
                        }
                    }
                }

                //if (!ckSocket.Dispatcher.CheckAccess())
                //else if (ckSocket.Dispatcher.CheckAccess())

                string sEntity = Json.Serialize<JsonData>(jsonData);

                if (itemOptions.ByType || JQ.Name == "" || JQ.Filters.Type.Filters.Rarity.Option != "unique")
                {
                    sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                    if (Inherit == "jewel" || itemOptions.ByType)
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                    else if (Inherit == "prophecy")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"name\":\"" + JQ.Type + "\",");
                    else if (JQ.Filters.Type?.Filters?.Category?.Option == "monster.sample")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"term\":\"" + JQ.Type + "\",");
                }

                sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}").Replace("{\"max\":99999,", "{").Replace(",\"min\":99999}", "}");

                sEntity = sEntity.Replace(",{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "");
                sEntity = sEntity.Replace("[{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "[").Replace("[,", "[");

                string tmp = "sale_type|rarity|category|corrupted|synthesised_item|shaper_item|elder_item|crusader_item|redeemer_item|hunter_item|warlord_item|map_shaped|map_elder|map_blighted";
                sEntity = Regex.Replace(sEntity, "\"(" + tmp + ")\":{\"option\":\"any\"},?", "").Replace("},}", "}}");

                // trade_filters 안의 빈 price:{} 제거
                sEntity = sEntity.Replace(",\"price\":{}", "").Replace("\"price\":{},", "");

                if (error_filter)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        (ThreadStart)delegate ()
                        {
                            for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                            {
                                if (itemOptions.itemfilters[i].isNull)
                                {
                                    ((TextBox)this.FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                    ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "error";
                                    ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "error";
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = false;
                                    ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                                }
                            }
                        }
                    );
                }

                return sEntity;
            }
            catch (Exception ex)
            {
                string logPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                logPath = logPath.Remove(logPath.Length - 4) + ".log";
                Exception cur = ex;
                while (cur != null)
                {
                    System.IO.File.AppendAllText(logPath, string.Format("[CREATEJSON] {0}: {1}\r\n{2}\r\n\r\n", cur.GetType().Name, cur.Message, cur.StackTrace));
                    cur = cur.InnerException;
                }
                return "";
            }
        }

        private void UpdatePrice(string[] entity, int listCount, System.Threading.CancellationToken ct = default)
        {
            string url_string = "";
            string json_entity = "";
            string msg = "정보가 없습니다";
            string msg_2 = "";

            try
            {
                if (entity.Length > 0 && !string.IsNullOrEmpty(entity[0]))
                {
                    if (entity.Length == 1)
                    {
                        json_entity = entity[0];
                        url_string = RS.TradeApi[RS.ServerLang] + RS.ServerType;

                        // search 보내기 전 강제 throttle: 한도 근접/차단이면 안전해질 때까지 대기.
                        WaitForRateLimit("trade-search-request-limit", ct);
                    }
                    string request_result = entity.Length == 1 ? SendHTTP(json_entity, url_string, mConfigData.Options.ServerTimeout) : null;

                    // ninja 화폐 시세 조회 (entity.Length > 1 = 화폐 교환 모드)
                    if (entity.Length > 1)
                    {
                        // 캐시가 없거나 30분 이상 지났으면 갱신
                        if (mNinjaCache.Count == 0 || (DateTime.Now - mNinjaLastFetch).TotalMinutes > 30)
                            FetchNinjaPrices();

                        double haveVal = GetNinjaDivineValue(entity[0]);
                        double wantVal = GetNinjaDivineValue(entity[1]);

                        ParserDictionary haveItem = GetExchangeItem(entity[0]);
                        ParserDictionary wantItem = GetExchangeItem(entity[1]);
                        string haveName = haveItem != null ? haveItem.Text[0] : entity[0];
                        string wantName = wantItem != null ? wantItem.Text[0] : entity[1];

                        if (haveVal > 0 && wantVal > 0)
                        {
                            double ratio = haveVal / wantVal;
                            string ratioStr = ratio >= 1
                                ? Math.Round(ratio, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)
                                : Math.Round(ratio, 4).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            double haveEx = haveVal * mNinjaExaltedRate;
                            string exRounded = (haveEx >= 1 ? Math.Round(haveEx, 1) : Math.Round(haveEx, 2)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            if (haveVal >= 1)
                            {
                                // 신성 이상: 엑잘 + 신성 표시
                                string divRounded = Math.Round(haveVal, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                msg = "1 " + haveName + " ≈ " + exRounded + " 엑잘티드 오브 = " + divRounded + " 신성한 오브";
                            }
                            else
                            {
                                // 신성 미만: 엑잘만 표시
                                msg = "1 " + haveName + " ≈ " + exRounded + " 엑잘티드 오브";
                            }
                        }
                        else if (haveVal <= 0)
                        {
                            msg = haveName + " 시세 정보 없음";
                        }

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                        {
                            cbPriceListTotal.Text = "";
                        });
                    }
                    else if (request_result == null)
                    {
                        msg = "거래소 접속이 원활하지 않습니다";
                    }
                    else if (request_result != null)
                    {
                        int total = 0;
                        int resultCount = 0;
                        Dictionary<string, int> currencys = new Dictionary<string, int>();
                        // 일반 trade API
                        {
                            ResultData resultData = Json.Deserialize<ResultData>(request_result);
                            resultCount = resultData.Result != null ? resultData.Result.Length : 0;

                            if (resultCount > 0)
                            {
                                for (int x = 0; x < listCount; x++)
                                {
                                    string[] tmp = new string[10];
                                    int cnt = x * 10;
                                    if (cnt >= resultData.Result.Length) break;
                                    for (int i = 0; i < 10; i++)
                                    {
                                        if (i + cnt >= resultData.Result.Length) break;
                                        tmp[i] = resultData.Result[i + cnt];
                                    }

                                    string json_result = "";
                                    string url = RS.FetchApi[RS.ServerLang] + string.Join(",", tmp) + "?query=" + Uri.EscapeDataString(resultData.ID);

                                    // fetch 보내기 전 강제 throttle: 거래소가 알려준 한도/차단에 맞춰 대기.
                                    WaitForRateLimit("trade-fetch-request-limit", ct);

                                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                    request.CookieContainer = new CookieContainer();
                                    request.UserAgent = RS.UserAgent;
                                    request.Timeout = mConfigData.Options.ServerTimeout * 1000;

                                    try
                                    {
                                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                        {
                                            CaptureRateLimit(response);
                                            json_result = streamReader.ReadToEnd();
                                        }
                                    }
                                    catch (WebException wex)
                                    {
                                        // 429 등: 헤더로 차단시각 반영 후 이 페이지는 건너뛴다.
                                        // using으로 응답을 닫아 연결 풀 고갈 방지.
                                        using (HttpWebResponse resp = wex.Response as HttpWebResponse)
                                        {
                                            if (resp != null) CaptureRateLimit(resp);
                                        }
                                        json_result = "";
                                    }

                                    if (json_result != "")
                                    {
                                        FetchData fetchData = Json.Deserialize<FetchData>(json_result);
                                        for (int i = 0; i < fetchData.Result.Length; i++)
                                        {
                                            if (fetchData.Result[i] == null) break;
                                            if (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)
                                            {
                                                string key = "";
                                                string indexed = fetchData.Result[i].Listing.Indexed;
                                                string account = fetchData.Result[i].Listing.Account.Name;
                                                string currency = fetchData.Result[i].Listing.Price.Currency;
                                                double amount = fetchData.Result[i].Listing.Price.Amount;

                                                liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                                                {
                                                    ParserDictionary item = GetExchangeItem(currency);
                                                    string keyName = item != null ? item.Text[0] : currency;
                                                    liPrice.Items.Add(String.Format("{0} {1} [{2}]",
                                                        GetLapsedTime(indexed).PadRight(10, ' '),
                                                        (amount + " " + keyName).PadRight(12, ' '),
                                                        account));
                                                });

                                                key = Math.Round(amount - 0.1) + " " + currency;
                                                if (currencys.ContainsKey(key)) currencys[key]++;
                                                else currencys.Add(key, 1);
                                                total++;
                                            }
                                        }
                                    }

                                    ct.ThrowIfCancellationRequested();
                                }

                                if (currencys.Count > 0)
                                {
                                    List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>(currencys);
                                    string first = myList[0].Key;
                                    string last = myList[myList.Count - 1].Key;
                                    myList.Sort((a, b) => -1 * a.Value.CompareTo(b.Value));
                                    for (int i = 0; i < myList.Count; i++)
                                    {
                                        if (i == 2) break;
                                        if (myList[i].Value < 2) continue;
                                        msg_2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                    }
                                    msg = first + " ~ " + last;
                                    msg_2 = msg_2.TrimEnd(',', ' ');
                                    if (msg_2 == "") msg_2 = "가장 많은 수 없음";
                                }
                            }

                            cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                            {
                                cbPriceListTotal.Text = total + "/" + resultCount + " 검색";
                            });

                            if (resultData.Total == 0 || currencys.Count == 0)
                                msg = mLockUpdatePrice ? "해당 물품의 거래가 없습니다" : "검색 실패: 클릭하여 다시 시도해주세요";
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
                Exception cur = ex;
                while (cur != null)
                {
                    System.IO.File.AppendAllText(logPath, string.Format("[UPDATEPRICE] {0}: {1}\r\n{2}\r\n\r\n", cur.GetType().Name, cur.Message, cur.StackTrace));
                    cur = cur.InnerException;
                }
            }
            finally
            {
                mLockUpdatePrice = false;

                tkPriceCount.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (tkPriceCount.Text == ".") tkPriceCount.Text = ""; // 값 . 이면 읽는중 표시 끝나면 처리
                });

                tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    tkPriceInfo.Text = msg + (msg_2 != "" ? " = " + msg_2 : "");
                });

                liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                    {
                        liPrice.Items.Add(msg + (msg_2 != "" ? " = " + msg_2 : ""));
                    }
                });
            }
        }

        private Thread priceThread = null;
        private System.Threading.CancellationTokenSource mPriceCts = null;
        private void UpdatePriceThreadWorker(ItemOption itemOptions, string[] exchange)
        {
            if (!mLockUpdatePrice)
            {
                mLockUpdatePrice = true;

                int listCount = (cbPriceListCount.SelectedIndex + 1) * 2;

                liPrice.Items.Clear();
                tkPriceCount.Text = ".";
                tkPriceInfo.Text = "시세 확인중...";
                cbPriceListTotal.Text = "0/0 검색";

                mPriceCts?.Cancel();
                mPriceCts = new System.Threading.CancellationTokenSource();
                var token = mPriceCts.Token;

                priceThread = new Thread(() =>
                {
                    try
                    {
                        UpdatePrice(
                            exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true) },
                            listCount,
                            token
                        );

                        if (!token.IsCancellationRequested && mConfigData.Options.AutoSearchDelay > 0 && exchange == null)
                        {
                            // 거래소가 차단 중이면 다음 자동검색을 차단 해제 후로 미룬다(무의미한 대기-검색 반복 방지).
                            int blockSec = (int)Math.Ceiling(RateLimit.BlockedSeconds("trade-search-request-limit"));
                            int delay = Math.Max(mConfigData.Options.AutoSearchDelay, blockSec);
                            mAutoSearchTimer.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                            {
                                mAutoSearchTimer.Stop();
                                mAutoSearchTimerCount = delay;
                                mAutoSearchTimer.Start();
                            });
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        string logPath = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".log");
                        Exception cur = ex;
                        while (cur != null)
                        {
                            System.IO.File.AppendAllText(logPath, string.Format("[THREAD] {0}: {1}\r\n{2}\r\n\r\n", cur.GetType().Name, cur.Message, cur.StackTrace));
                            cur = cur.InnerException;
                        }
                    }
                });
                priceThread.IsBackground = true;
                priceThread.Start();
            }
        }

        // 요청 보내기 전 거래소 rate limit에 맞춰 강제 대기.
        // 백그라운드 검색 스레드에서 호출된다(UI 스레드 아님 → Sleep 허용).
        // 대기 중에는 tkPriceInfo에 남은 초를 표시하고, 취소되면 즉시 중단한다.
        private void WaitForRateLimit(string policy, System.Threading.CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                double wait = RateLimit.SecondsToWait(policy);
                if (wait <= 0) break;

                // 너무 긴 대기는 1초 단위로 쪼개 카운트다운 표시 + 취소 반응.
                int shown = (int)Math.Ceiling(wait);
                string label = RateLimit.BlockedSeconds(policy) > 0
                    ? "거래소 요청 제한 — " + shown + "초 후 재개"
                    : "거래소 혼잡 — " + shown + "초 대기";
                tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    tkPriceInfo.Text = label;
                });

                System.Threading.Thread.Sleep(wait > 1 ? 1000 : (int)(wait * 1000) + 50);
            }
        }

        private int mAutoSearchTimerCount;
        private void AutoSearchTimer_Tick(object sender, EventArgs e)
        {
            tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
            {
                if (mAutoSearchTimerCount < 1)
                {
                    mAutoSearchTimer.Stop();
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag;
                }
                else
                {
                    mAutoSearchTimerCount--;
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag + " (" + mAutoSearchTimerCount + ")";
                }
            });
        }

        private List<string> ExtractListingBlocks(string json)
        {
            var blocks = new List<string>();
            int search = 0;
            while (true)
            {
                int idx = json.IndexOf("\"listing\"", search);
                if (idx < 0) break;
                int brace = json.IndexOf('{', idx + 9);
                if (brace < 0) break;
                int depth = 0, i = brace;
                bool inStr = false, esc = false;
                while (i < json.Length)
                {
                    char c = json[i];
                    if (esc) { esc = false; }
                    else if (c == '\\' && inStr) { esc = true; }
                    else if (c == '"') { inStr = !inStr; }
                    else if (!inStr)
                    {
                        if (c == '{') depth++;
                        else if (c == '}') { depth--; if (depth == 0) { blocks.Add(json.Substring(brace, i - brace + 1)); search = i + 1; break; } }
                    }
                    i++;
                }
                if (depth != 0) break;
            }
            return blocks;
        }

private ParserDictionary GetExchangeItem(string id)
        {
            ParserDictionary item = Array.Find(mParserData.Currency.Entries, x => x.Id == id);
            if (item == null)
                item = Array.Find(mParserData.Exchange.Entries, x => x.Id == id);

            if (item == null && mStaticData[0]?.Result != null)
            {
                foreach (var group in mStaticData[0].Result)
                {
                    var entry = Array.Find(group.Entries, x => x.Id == id);
                    if (entry != null)
                    {
                        item = new ParserDictionary { Text = new string[] { entry.Text, entry.Text } };
                        break;
                    }
                }
            }

            return item;
        }

        private ParserDictionary GetExchangeItem(int index, string text)
        {
            ParserDictionary item = Array.Find(mParserData.Currency.Entries, x => x.Text[index] == text);
            if (item == null)
                item = Array.Find(mParserData.Exchange.Entries, x => x.Text[index] == text);

            if (item == null && mStaticData[0]?.Result != null)
            {
                foreach (var group in mStaticData[0].Result)
                {
                    var entry = Array.Find(group.Entries, x => x.Text == text);
                    if (entry != null)
                    {
                        item = new ParserDictionary { Id = entry.Id, Text = new string[] { entry.Text, entry.Text } };
                        break;
                    }
                }
            }

            return item;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
            {
                if (!mInstalledHotKey)
                    InstallRegisterHotKey();

            }
            else
            {
                if (mInstalledHotKey)
                    RemoveRegisterHotKey();
            }
        }

        private void InstallRegisterHotKey()
        {
            mInstalledHotKey = true;

            // 0x0: None, 0x1: Alt, 0x2: Ctrl, 0x3: Shift
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.RegisterHotKey(mMainHwnd, 10001 + i, (uint)(shortcut.Ctrl ? 0x2 : 0x0), (uint)Math.Abs(shortcut.Keycode));
            }
        }

        private void RemoveRegisterHotKey()
        {
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.UnregisterHotKey(mMainHwnd, 10001 + i);
            }

            mInstalledHotKey = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DRAWCLIPBOARD)
            {
                if (!mPausedHotKey && !mClipboardBlock)
                {
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
                    {
                        try
                        {
                            if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                        }
                        catch (Exception ex)
                        {
                            string logPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            logPath = logPath.Remove(logPath.Length - 4) + ".log";
                            Exception cur = ex;
                            while (cur != null)
                            {
                                System.IO.File.AppendAllText(logPath, string.Format("[WNDPROC] {0}: {1}\r\n{2}\r\n\r\n", cur.GetType().Name, cur.Message, cur.StackTrace));
                                cur = cur.InnerException;
                            }
                        }
                    }
                }
            }
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = Native.FindWindow(RS.PoeClass, RS.PoeCaption);

                if (Native.GetForegroundWindow().Equals(findHwnd))
                {
                    int key_idx = wParam.ToInt32() - 10001;

                    try
                    {
                        ConfigShortcut shortcut = mConfigData.Shortcuts[key_idx];

                        if (shortcut != null && shortcut.Value != null)
                        {
                            string valueLower = shortcut.Value.ToLower();

                            if (valueLower == "{pause}")
                            {
                                mPausedHotKey = !mPausedHotKey;

                                if (mPausedHotKey)
                                {
                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 일시 중지합니다." + '\n'
                                                    + "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                }
                                else
                                {
                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 다시 시작합니다.", "POE 거래소 검색");
                                }

                                Native.SetForegroundWindow(findHwnd);
                            }
                            else if (valueLower == "{close}")
                            {
                                if (this.Visibility == Visibility.Hidden)
                                {
                                    Native.SendMessage(findHwnd, 0x0101, new IntPtr(shortcut.Keycode), IntPtr.Zero);
                                }
                                else if (this.Visibility == Visibility.Visible)
                                {
                                    Close();
                                }
                            }
                            else if (!mPausedHotKey)
                            {
                                if (valueLower == "{run}")
                                {
                                    mClipboardBlock = true;

                                    System.Windows.Forms.SendKeys.SendWait("^{c}");
                                    Thread.Sleep(300);

                                    try
                                    {
                                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    mClipboardBlock = false;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ForegroundMessage("잘못된 단축키 명령입니다.", "단축키 에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }
    }
}
