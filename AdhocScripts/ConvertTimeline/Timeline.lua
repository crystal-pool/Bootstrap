local data = {
	--松星的抉择
	["pc"] = {
		bookName = '松星的抉择',
		interval = {"1", "3", "4", "6", "7", "8"},
		details = {
			["1"] = { year=11,  month=3 }, -- 第1章－11夏
			["3"] = { year=11,  month=9 }, -- 第3章－11冬
			["4"] = { year=18,  month=0 }, -- 第4章－18春（《鹅羽》结束）
			["6"] = { year=20,  month=6 }, -- 第6章－20秋（月花之死）
			["7"] = { year=21,  month=0 }, -- 第7章－21春（蓝毛成为武士）
			["8"] = { year=21,  month=3 }, -- 第8章－21夏（小虎出生）
		}
	},

	--鹅羽的诅咒
	["gc"] = {
		bookName = '鹅羽的诅咒',
		interval = {"1", "5", "6", "7", "8", "10"},
		details = {
			["1"] = { year=18,  month=3 }, -- 第1章－？
			["5"] = { year=18,  month=3 }, -- 第5章－18夏
			["6"] = { year=18,  month=6 }, -- 第6章－18夏末秋初
			["7"] = { year=18,  month=9 }, -- 第7章－18秋末秋冬
			["8"] = { year=18,  month=9 }, -- 第8章－18冬
			["10"] = { year=19,  month=0 }, -- 第10章－19春
		}
	},

	--高星的复仇
	["tr"] = {
		bookName = '高星的复仇',
		interval = {"1", "3", "5", "21", "24", "41", "44"},
		details = {
			["1"] = { year=18,  month=9 }, -- 第1章－18冬
			["3"] = { year=19,  month=0 }, -- 第3章－19春
			["5"] = { year=19,  month=3 }, -- 第5章－19夏
			["21"] = { year=19,  month=6 }, -- 第21章－19秋
			["24"] = { year=19,  month=9 }, -- 第24章－19秋末近冬
			["41"] = { year=19,  month=11 }, -- 第41章－19冬末近春
			["44"] = { year=20,  month=0 }, -- 第44章－20春
			--["45"] = { year=20,  month=0 }, -- 第45章－？
		}
	},

	--黄牙的秘密
	["ys"] = {
		bookName = '黄牙的秘密',
		interval = {"1", "4", "8", "8.6", "13", "14", "22", "24", "28", "29", "30", "31", "32", "33", "34", "38", "39"},
		details = {
			["1"] = { year=19,  month=3 }, -- 第1章－19夏
			["4"] = { year=19,  month=9 }, -- 第4章－19冬（下雪了）
			["8"] = { year=20,  month=0 }, -- 第8章－20春（垃圾场行动两个月后）
			["8.6"] = { year=20,  month=3 }, -- 第8章－20夏（黄牙获得武士名）
			["13"] = { year=20,  month=6 }, -- 第13章－20秋
			["14"] = { year=20,  month=9 }, -- 第13章－20冬
			["22"] = { year=21,  month=6 }, -- 第22章－21秋
			["24"] = { year=21,  month=9 }, -- 第24章－21冬
			["28"] = { year=22,  month=0 }, -- 第28章－22春
			["29"] = { year=22,  month=3 }, -- 第29章－22夏
			["30"] = { year=22,  month=6 }, -- 第30章－22秋
			["31"] = { year=22,  month=9 }, -- 第31章－22冬
			["32"] = { year=23,  month=0 }, -- 第32章－23春
			["33"] = { year=24,  month=3 }, -- 第33章－24夏
			["34"] = { year=24,  month=6 }, -- 第34章－24秋
			["38"] = { year=25,  month=0 }, -- 第38章－25春
			["39"] = { year=25,  month=3 }, -- 第38章－25夏
		}
	},

	--钩星的承诺
	["cp"] = {
		bookName = '钩星的承诺',
		interval = {"pr", "1", "5", "9", "12", "16", "20", "27", "28", "29", "34", "35", "38"},
		details = {
			["pr"] = { year=19,  month=9 }, -- 引子－19冬
			["1"] = { year=20,  month=0 }, -- 第1章－20春
			["5"] = { year=20,  month=3 }, -- 第5章－20春末夏初
			["9"] = { year=20,  month=6 }, -- 第9章－20秋
			["12"] = { year=20,  month=9 }, -- 第12章－20冬
			["16"] = { year=21,  month=0 }, -- 第16章－21春
			["20"] = { year=21,  month=3 }, -- 第20章－21夏
			["27"] = { year=21,  month=6 }, -- 第27章－21秋
			["28"] = { year=21,  month=9 }, -- 第28章－21冬
			["29"] = { year=22,  month=3 }, -- 第29章－22夏
			["34"] = { year=22,  month=6 }, -- 第34章－22秋
			["35"] = { year=22,  month=9 }, -- 第35章－22冬
			["38"] = { year=24,  month=3 }, -- 第38章－24夏
		}
	},
	
	--蓝星的预言
	["bp"] = {
		bookName = '蓝星的预言',
		interval = {"1", "3", "6", "10", "14", "18", "25", "32", "33", "38", "44", "45"},
		details = {
			["1"] = { year=20,  month=0 }, -- 第1章－20春初
			["3"] = { year=20,  month=6 }, -- 第3章－20秋
			["6"] = { year=20,  month=8 }, -- 第6章－20秋末
			["10"] = { year=20,  month=9 }, -- 第10章－20冬
			["14"] = { year=21,  month=0 }, -- 第14章－21春
			["18"] = { year=21,  month=3 }, -- 第18章－21夏
			["25"] = { year=21,  month=6 }, -- 第25章－21秋
			["32"] = { year=21,  month=9 }, -- 第32章－21冬
			["33"] = { year=22,  month=6 }, -- 第33章－22秋
			["38"] = { year=22,  month=9 }, -- 第38章－22冬
			["44"] = { year=23,  month=0 }, -- 第44章－23春？ （无法判定）
			["45"] = { year=25,  month=0 }, -- 第45章－25春 = 呼唤野性
		}
	},

	--预言开始
	["os1"] = {
		bookName = '呼唤野性',
		interval = {"1", "6"},
		details = {
			["1"] = { year=25,  month=0 }, -- 第1章－25春
			["6"] = { year=25,  month=3 }, -- 第6章－25夏
		}
	},
	["os2"] = {
		bookName = '寒冰烈火',
		interval = {"1", "13"},
		details = {
			["1"] = { year=25,  month=6 }, -- 第1章－25秋
			["13"] = { year=25,  month=9 }, -- 第13章－25冬
		}
	},
	["os3"] = {
		bookName = '疑云重重',
		interval = {"1"},
		details = {
			["1"] = { year=26,  month=0 }, -- 第1章－26春
		}
	},
	["os4"] = {
		bookName = '风起云涌',
		interval = {"1"},
		details = {
			["1"] = { year=26,  month=3 }, -- 第1章－26夏
		}
	},
	["os5"] = {
		bookName = '险路惊魂',
		interval = {"1"},
		details = {
			["1"] = { year=26,  month=6 }, -- 第1章－26秋
		}
	},
	["os6"] = {
		bookName = '力挽狂澜',
		interval = {"1"},
		details = {
			["1"] = { year=26,  month=9 }, -- 第1章－26冬
		}
	},

	["tf"] = {
		bookName = '虎掌的憤怒',
		interval = {"1"},
		details = {
			["1"] = { year=26,  month=3 }, -- 第1章－26夏
		}
	},
	["ts3"] = {
		bookName = '返回族群',
		interval = {"1", "2"},
		details = {
			["1"] = { year=26,  month=9 }, -- Pt.1－26冬
			["2"] = { year=27,  month=0 }, -- Pt.2－27春
		}
	},

	["fq"] = {
		bookName = '火星的探索',
		interval = {"1", "31", "ep"},
		details = {
			["1"] = { year=27,  month=3 }, -- 第1章－27夏
			["31"] = { year=27,  month=6 }, -- 第31章－27秋
			["ep"] = { year=27,  month=9 }, -- 尾声－27冬
		}
	},

	--新预言
	["np1"] = {
		bookName = '午夜追踪',
		interval = {"1", "10"},
		details = {
			["1"] = { year=28,  month=3 }, -- 第1章－28夏
			["10"] = { year=28,  month=6 }, -- 第10章－28秋
		}
	},
	["np2"] = {
		bookName = '新月危机',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=6 }, -- 第1章－28秋
		}
	},
	["np3"] = {
		bookName = '重现家园',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=9 }, -- 第1章－28冬
		}
	},
	["np4"] = {
		bookName = '星光指路',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=10 }, -- 第1章－28冬（？）
		}
	},
	["np5"] = {
		bookName = '黄昏战争',
		interval = {"1"},
		details = {
			["1"] = { year=29,  month=0 }, -- 第1章－29春
		}
	},
	["np6"] = {
		bookName = '日落和平',
		interval = {"1"},
		details = {
			["1"] = { year=29,  month=2 }, -- 第1章－29春末
		}
	},

	--三力量
	["po1"] = {
		bookName = '预视力量',
		interval = {"1"},
		details = {
			["1"] = { year=29,  month=9 }, -- 第1章－29冬
		}
	},
	["po2"] = {
		bookName = '暗河汹涌',
		interval = {"1"},
		details = {
			["1"] = { year=30,  month=0 }, -- 第1章－30春
		}
	},
	["po3"] = {
		bookName = '驱逐之战',
		interval = {"1","26"},
		details = {
			["1"] = { year=30,  month=0 }, -- 第1章－30春
			["26"] = { year=30,  month=3 }, -- 第26章－30夏
		}
	},
	["po4"] = {
		bookName = '天蚀遮月',
		interval = {"1","2"},
		details = {
			["1"] = { year=30,  month=3 }, -- 第1章－30夏（？）
			["2"] = { year=30,  month=6 }, -- 第2章－30秋
		}
	},
	["po5"] = {
		bookName = '暗夜长影',
		interval = {"1"},
		details = {
			["1"] = { year=30,  month=6 }, -- 第1章－30秋
		}
	},
	["po6"] = {
		bookName = '拂晓之光',
		interval = {"1"},
		details = {
			["1"] = { year=30,  month=9 }, -- 第1章－30冬
		}
	},

	--星预言
	["om1"] = {
		bookName = '第四学徒',
		interval = {"1"},
		details = {
			["1"] = { year=31,  month=3 }, -- 第1章－31夏
		}
	},
	["om2"] = {
		bookName = '战声渐近',
		interval = {"1"},
		details = {
			["1"] = { year=31,  month=6 }, -- 第1章－31秋
		}
	},
	["om3"] = {
		bookName = '暗夜密语',
		interval = {"1"},
		details = {
			["1"] = { year=31,  month=9 }, -- 第1章－31冬（？）
		}
	},
	["om4"] = {
		bookName = '月光印记',
		interval = {"1"},
		details = {
			["1"] = { year=31,  month=9 }, -- 第1章－31冬
		}
	},
	["om5"] = {
		bookName = '武士归来',
		interval = {"1","9"},
		details = {
			["1"] = { year=32,  month=0 }, -- 第1章－32春
			["9"] = { year=32,  month=3 }, -- 第9章－32夏（？）
		}
	},
	["om6"] = {
		bookName = '群星之战',
		interval = {"1","19"},
		details = {
			["1"] = { year=32,  month=4 }, -- 第1章－32夏中
			["19"] = { year=32,  month=6 }, -- 第19章－32夏末秋初
		}
	},

	["mo"] = {
		bookName = '雾星的征兆',
		interval = {"1"},
		details = {
			["1"] = { year=31,  month=6 }, -- 第1章－31秋
		}
	},
	["rf"] = {
		bookName = '乌爪的告别',
		interval = {"1", "3", "4"},
		details = {
			["1"] = { year=31,  month=6 }, -- 第1章－31秋
			["3"] = { year=31,  month=9 }, -- 第3章－31冬
			["4"] = { year=32,  month=0 }, -- 第3章－32春
		}
	},
	["ds"] = {
		bookName = '鸽翅的沉默',
		interval = {"1", "6"},
		details = {
			["1"] = { year=32,  month=6 }, -- 第1章－32夏末秋初
			["6"] = { year=32,  month=9 }, -- 第6章－32冬（？）
		}
	},
	["bs"] = {
		bookName = '黑莓星的风暴',
		interval = {"1","m"},
		details = {
			["1"] = { year=33,  month=0 }, -- 第1章－33春
			["m"] = { year=33,  month=9 }, -- 漫画－33冬
		}
	},

	--暗影幻象
	["vs1"] = {
		bookName = '学徒探索',
		interval = {"1"},
		details = {
			["1"] = { year=34,  month=3 }, -- 第1章－34夏
		}
	},
	["vs2"] = {
		bookName = '雷影交加',
		interval = {"1","12","16"},
		details = {
			["1"] = { year=34,  month=6 }, -- 第1章－34秋
			["12"] = { year=34,  month=9 }, -- 第1章－34冬
			["16"] = { year=35,  month=0 }, -- 第1章－35春（？）
		}
	},
	["vs3"] = {
		bookName = '天空破碎',
		interval = {"1"},
		details = {
			["1"] = { year=35,  month=0 }, -- 第1章－35春
		}
	},
	["vs4"] = {
		bookName = '极夜无光',
		interval = {"1","11"},
		details = {
			["1"] = { year=35,  month=3 }, -- 第1章－35夏
			["11"] = { year=35,  month=6 }, -- 第1章－35秋
		}
	},
	["vs5"] = { -- todo
		bookName = '烈火焚河',
		interval = {"1"},
		details = {
			["1"] = { year=35,  month=7 }, -- 第?章－35秋 （接vs4尾 待判定）
		}
	},
	["vs6"] = {
		bookName = '风暴来袭',
		interval = {"1"},
		details = {
			["1"] = { year=36,  month=0 }, -- 第?章－36春 （待判定）
		}
	},

	--破灭守则
	["bc1"] = {
		bookName = '迷失群星',
		interval = {"1"},
		details = {
			["1"] = { year=36,  month=9 }, -- 第?章－36冬 （待判定）
		}
	},

	["ts"] = {
		bookName = '虎心的阴影',
		interval = {"1", "19", "33"},
		details = {
			["1"] = { year=35,  month=6 }, -- 第1章－35秋
			["19"] = { year=35,  month=9 }, -- 第19章－35冬
			["33"] = { year=35,  month=11 }, -- 第33章－35冬末（离开城市两月）
		}
	},

	-- SkyClan
	["sd"] = {
		bookName = '天族外传',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=0 }, -- 第1章－28春
		}
	},
	["ss1"] = {
		bookName = '救援',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=9 }, -- 第1章－28冬
		}
	},
	["ss2"] = {
		bookName = '超越守则',
		interval = {"1"},
		details = {
			["1"] = { year=28,  month=10 }, -- 第1章－28冬（？）
		}
	},
	["ss3"] = {
		bookName = '超越守则',
		interval = {"1"},
		details = {
			["1"] = { year=29,  month=0 }, -- 第1章－29春
		}
	},
	["hj"] = { -- todo
		bookName = '鹰翅的旅程',
		interval = {"1", "15", "29", "29.5", "30", "32", "m"},
		details = {
			["1"] = { year=33,  month=3 }, -- 第1章－33夏
			["15"] = { year=34,  month=0 }, -- 第15章(?)－34春
			["29"] = { year=34,  month=3 }, -- 第29章(?)－34夏
			["29.5"] = { year=34,  month=4 }, -- 第29章(?)－34夏+1
			["30"] = { year=34,  month=6 }, -- 第30章(?)－34秋
			["32"] = { year=34,  month=9 }, -- 第32章(?)－34冬
			["m"] = { year=35,  month=0 }, -- 漫画－35春
		}
	},

	-- special (todo)
	["now"] = { --最新进度：迷失群星
		bookName = '迷失群星',
		interval = {"1"},
		details = {
			["1"] = { year=36,  month=9 }, -- 第?章－36冬 （待判定）
		}
	},
	["nowTS"] = { --最新进度：虎心的阴影 （待合并）
		bookName = '虎心的阴影',
		interval = {"1"},
		details = {
			["1"] = { year=35,  month=12 }, -- 第35章－35冬
		}
	},
}

return data
