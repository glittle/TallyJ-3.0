/* 

Title:
  Today in the Badi calendar.

Purpose:
  To show the current date from the Badi calendar.
  Designed as single file to be incorporated into any web site.
  The date shown is calculated in the browser, using it's current date and location.
  Please note, this does not calculate or show Holy Days for the Baha'i Faith.
  For more advanced scripts, see https://sites.google.com/site/badicalendartools/  
  
Usage:
  BadiDateToday({
    onReady: showInfo
  });
  function showInfo(di){
    console.log('{bDay} {bMonthMeaning} ({bMonthNameAr}) {bYear} {bEraAbbrev}'.filledWith(di)); 
        // --> results in, for example: "7 Words (Kalimát) 172 B.E."
  }

Optional Settings:
  var myVariable = BadiDateToday({
    language: 'fr',
    onReady: showInfo
  });
  Settings can include:
    onReady - a function that is called when the date is determined. One parameter is passed.
    language - 'en' or one of the languages defined below in _messages
    locationIdentification - one of the BadiDateLocationChoice options
    use24HourClock - true or false - used when displaying time
    currentTime - used for testings - if not supplied, current time is used


Help:
  In the browser console, type:  
      var di = myVariable.getDateInfo() 
  to see all available tags that can be used.

  Tips on tag names:
    "____Ar" means the Arabic name
    "____Meaning" is the English or other local language translation/meaning of the name
    "g_____" means the Gregorian calendar
    "current______" is this moment in the Gregorian calendar

  Each tag can also be accessed directly:  di.bDay

Translation:
  To translate to another language, duplicate the "French" section of _messages below, and supply your language's words. If a message is not 
  translated, English will be used. Also set settings "language" to the language code you have defined.
   
*/

// possible settings for "locationMethod"
var BadiDateLocationChoice = {
    // don't try to customize sunset times, just use 6:30pm
    ignoreLocation: 1,
    // guess the user's location
    //guessUserLocation: 2,
    // ask the user for their location. User will be prompted to allow your site to read their location.
    askForUserLocation: 3
    // if location is reused, this is set to 4
}

var BadiDateToday = function (settings) {
    var version = 'v1.08';
    var localSettings = {};
    applySettings(settings);

    if (settings && settings.onReady) {
        // if user supplied onReady, but didn't set locationMethod, set it to guessUserLocation
        if (!settings.locationMethod) {
            settings.locationMethod = BadiDateLocationChoice.askForUserLocation;
        }
    }

    var _messages = {
        en: {
            "bYearInVahidMeaning": { "message": ",A,B,Father,D,Gate,V,Eternity,Generosity,Splendour,Love,Delightful,Answer,Single,Bountiful,Affection,Beginning,Luminous,Most Luminous,Unity", "description": "Vahid names, with leading , " },
            "bMonthMeaning": { "message": "Intercalary Days,Splendor,Glory,Beauty,Grandeur,Light,Mercy,Words,Perfection,Names,Might,Will,Knowledge,Power,Speech,Questions,Honor,Sovereignty,Dominion,Loftiness", "description": "Names of days/months, with first one being Ayyam-i-ha" },
            "bWeekdayMeaning": { "message": ",Glory,Beauty,Perfection,Grace,Justice,Majesty,Independence", "description": "start with Saturday" },
            "gWeekdayLong": { "message": "Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday", "description": "start with Sunday" },
            "gWeekdayShort": { "message": "Sun,Mon,Tue,Wed,Thu,Fri,Sat", "description": "start with Sun" },
            "gMonthLong": { "message": "January,February,March,April,May,June,July,August,September,October,November,December", "description": "" },
            "gMonthShort": { "message": "Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec", "description": "" },

            "eraLong": { "message": "Bahá'í Era", "description": "" },
            "eraAbbrev": { "message": "B.E.", "description": "" },
            "eraShort": { "message": "BE", "description": "" },
            "ordinal": { "message": "th,st,nd,rd", "description": "x,1,2,3" },
            "ordinalNames": { "message": ",first,second,third,fourth,fifth,sixth,seventh,eighth,nineth,tenth,eleventh,twelfth,thirteenth,fourteenth,fifteenth,sixteenth,seventeenth,eighteenth,nineteenth", "description": "" },
            "nearestSunsetEve": { "message": "{dayStarted} with sunset at {startingSunsetDesc}", "description": "" },
            "nearestSunsetDay": { "message": "{dayEnded} with sunset at {endingSunsetDesc}", "description": "" },

            "afterSunset": { "message": "evening - after sunset", "description": "" },
            "beforeSunset": { "message": "day - before sunset", "description": "" },
            "dayStartedFuture": { "message": "Starts", "description": "" },
            "dayStartedPast": { "message": "Started", "description": "" },
            "dayEndedFuture": { "message": "Ends", "description": "" },
            "dayEndedPast": { "message": "Ended", "description": "" },

            "elements": { "message": "Fire,Air,Water,Earth", "description": "the Bab's designations, found in 'https://books.google.ca/books?id=XTfoaK15t64C&pg=PA394&lpg=PA394&dq=get+of+the+heart+nader+bab&source=bl&ots=vyF-pWLAr8&sig=ruiuoE48sGWWgaB_AFKcSfkHvqw&hl=en&sa=X&ei=hbp0VfGwIon6oQSTk4Mg&ved=0CDAQ6AEwAw#v=snippet&q=%22air%20of%20eternity%22&f=false' " },

            "gMonthDayYear": { "message": "{bMonthNameAr} {bDay}, {bYear}", "description": "" },
            "gCombined_1": { "message": "{frag1MonthShort} {frag1Day}/{frag2Day}", "description": "day in same gregorian month" },
            "gCombinedY_1": { "message": "{frag1MonthShort} {frag1Day}/{frag2Day}, {frag1Year}", "description": "with year" },
            "gCombined_2": { "message": "{frag1MonthShort} {frag1Day}/{frag2MonthShort} {frag2Day}", "description": "day spanning different gregorian months" },
            "gCombinedY_2": { "message": "{frag1MonthShort} {frag1Day}/{frag2MonthShort} {frag2Day}, {frag1Year}", "description": "with year" },
            "gCombined_3": { "message": "{frag1MonthShort} {frag1Day}/{frag2MonthShort} {frag2Day}", "description": "day spanning different gregorian years" },
            "gCombinedY_3": { "message": "{frag1MonthShort} {frag1Day}, {frag1Year}/{frag2MonthShort} {frag2Day}, {frag2Year}", "description": "with year" },
        },
        fr: {
            "bYearInVahidMeaning": { "message": ",A,B,Père,D,Porte,V,Éternité,Générosité,Splendeur,Amour,Délice,Réponse,Unique,Bienfaisant,Affection,Commencement,Lumineux,Le Plus Lumineux,Unité", "description": "Vahid names, with leading , " },
            "bMonthMeaning": { "message": "Jours Intercalaires,Splendeur,Gloire,Beauté,Grandeur,Lumière,Miséricorde,Paroles,Perfection,Noms,Puissance,Volonté,Connaissance,Pouvoir,Discours,Questions,Honneur,Souveraineté,Empire,Élévation", "description": "Names of days/months, with first one being Ayyam-i-ha" },
            "bWeekDayMeaning": { "message": ",Gloire,Beauté,Perfection,Grâce,Justice,Majesté,Indépendance", "description": "start with Saturday" },
            "gWeekdayLong": { "message": "Dimanche,Lundi,Mardi,Mercredi,Jeudi,Vendredi,Samedi", "description": "start with Sunday" },
            "gWeekdayShort": { "message": "Dim,Lun,Mar,Mer,Jeu,Ven,Sam", "description": "start with Sun" },
            "gMonthLong": { "message": "janvier,février,mars,avril,mai,juin,juillet,août,septembre,octobre,novembre,décembre", "description": "" },
            "gMonthShort": { "message": "jan,fév,mars,avr,mai,juin,juil,août,sept,oct,nov,déc", "description": "" },

            "eraLong": { "message": "Ère Bahá'íe", "description": "" },
            "eraAbbrev": { "message": "E.B.", "description": "" },
            "eraShort": { "message": "EB", "description": "" },
            "ordinal": { "message": "ème,er,ème,ème", "description": "x,1,2,3" },
            "ordinalNames": { "message": ",premier,deuxième,troisième,quatrième,cinquième,sixième,septième,huitième,neuvième,dixième,onzième,douzième,treizième,quatorzième,quinzième,seizième,dix-septième,dix-huitième,dix-neuvième", "description": "" },
            "nearestSunsetEve": { "message": "{dayStarted} au coucher du soleil à {startingSunsetDesc}", "description": "" },
            "nearestSunsetDay": { "message": "{dayEnded} au coucher du soleil à {endingSunsetDesc}", "description": "" },

            "afterSunset": { "message": "soir - après le coucher du soleil", "description": "" },
            "beforeSunset": { "message": "jour - avant le coucher du soleil", "description": "" },
            "dayStartedFuture": { "message": "Commencera", "description": "" },
            "dayStartedPast": { "message": "A commencé", "description": "" },
            "dayEndedFuture": { "message": "Finira", "description": "" },
            "dayEndedPast": { "message": "Est fini", "description": "" },

            "elements": { "message": "Feu,Air,Eau,Terre", "description": "temporary translation - please replace! The Bab's designations, found in 'https://books.google.ca/books?id=XTfoaK15t64C&pg=PA394&lpg=PA394&dq=get+of+the+heart+nader+bab&source=bl&ots=vyF-pWLAr8&sig=ruiuoE48sGWWgaB_AFKcSfkHvqw&hl=en&sa=X&ei=hbp0VfGwIon6oQSTk4Mg&ved=0CDAQ6AEwAw#v=snippet&q=%22air%20of%20eternity%22&f=false' " },
        },
        pt_PT: {
            "bYearInVahidMeaning": { "message": ",A,B,Pai,D,Porta,V,Eternidade,Generosidade,Esplendor,Amor,Deleite,Resposta,Único,Caridoso,Afecto,Início,Luminosidade,Mais Luminoso,Unidade", "description": "Nome Vahid começados por... " },
            "bMonthMeaning": { "message": "Dias Intercalares,Esplendor,Glória,Beleza,Grandeza,Luz,Misericórdia,Palavras,Perfeição,Nomes,Potência,Vontade,Conhecimento,Poder,Discurso,Perguntas,Honra,Soberania,Domínio,Sublimidade", "description": "Nomes dos dias/meses após Ayyam-i-ha" },
            "bWeekDayMeaning": { "message": ",Glória,Beleza,Perfeição,Graça,Justiça,Majestade,Independência", "description": "Inicia-se no Sábado" },
            "gWeekdayLong": { "message": "Domingo,Segunda-feira,Terça-feira,Quarta-feira,Quinta-feira,Sexta-feira,Sábado", "description": "Inicia-se no Domingo" },
            "gWeekdayShort": { "message": "Dom,Seg,Ter,Qua,Qui,Sex,Sab", "description": "Inicia-se no Dom" },
            "gMonthLong": { "message": "Janeiro,Fevereiro,Março,Abril,Maio,Junho,Julho,Agosto,Setembro,Outubo,Novembro,Dezembro", "description": "" },
            "gMonthShort": { "message": "Jan,Fev,Mar,Abr,Mai,Jun,Jul,Ago,Set,Out,Nov,Dez", "description": "" },

            "eraLong": { "message": "Era Bahá'í", "description": "" },
            "eraAbbrev": { "message": "E.B.", "description": "" },
            "eraShort": { "message": "EB", "description": "" },
            "ordinal": { "message": "º", "description": "x,1,2,3" },
            "ordinalNames": { "message": ",primeiro,segundo,terceiro,quarto,quinto,sexto,sétimo,oitavo,nono,décimo,décimo primeiro,décimo segundo,décimo terceiro,décimo quarto,décimo quinto,décimo sexto,décimo sétimo,décimo oitavo,décimo nono", "description": "" },
            "nearestSunsetEve": { "message": "{dayStarted} com o pôr-do-sol às {startingSunsetDesc}", "description": "" },
            "nearestSunsetDay": { "message": "{dayEnded} com o pôr-do-sol às {endingSunsetDesc}", "description": "" },

            "afterSunset": { "message": "noite - após pôr-do-sol", "description": "" },
            "beforeSunset": { "message": "dia - antes pôr-do-sol", "description": "" },
            "dayStartedFuture": { "message": "Inicia", "description": "" },
            "dayStartedPast": { "message": "Iniciado", "description": "" },
            "dayEndedFuture": { "message": "Termina", "description": "" },
            "dayEndedPast": { "message": "Terminado", "description": "" },

            "elements": { "message": "Fogo,Ar,Água,Terra", "description": " designações do Bab's , encontradas em https://books.google.ca/books?id=XTfoaK15t64C&pg=PA394&lpg=PA394&dq=get+of+the+heart+nader+bab&source=bl&ots=vyF-pWLAr8&sig=ruiuoE48sGWWgaB_AFKcSfkHvqw&hl=en&sa=X&ei=hbp0VfGwIon6oQSTk4Mg&ved=0CDAQ6AEwAw#v=snippet&q=%22air%20of%20eternity%22&f=false' " },
        }
    };

    try {
        console.log('Badíˋ Date - ' + version + ' - by Glen Little - https://sites.google.com/site/badicalendartools/');
    } catch (e) {
        // ignore in old browsers
    }

    var _locationLat = 0;
    var _locationLong = 0;
    var _locationName = null;
    var _locationKnown = false;
    var di = {};

    //if (WaitingForLocationInformation(function () {
    //  //
    //})) {
    //  return;
    //}
    function applySettings(newSettings) {
        localSettings.onReady = (newSettings && newSettings.onReady) || localSettings.onReady || defaultOnReady;
        localSettings.locationMethod = (newSettings && newSettings.locationMethod) || localSettings.originalLocationMethod || BadiDateLocationChoice.askForUserLocation;
        localSettings.use24HourClock = (newSettings && newSettings.use24HourClock) || localSettings.use24HourClock || false;
        localSettings.language = (newSettings && newSettings.language) || localSettings.language || 'en';
        localSettings.currentTime = (newSettings && newSettings.currentTime) || new Date(); // don't reuse time
    }

    function defaultOnReady(dateInfo) {
        var defaultTemplate = '{bDay} {bMonthNameAr} {bYear}';
        if ($ && $.jquery) {
            var defaultSelector = '.BadiDate';
            var target = $(defaultSelector);
            target.each(function (i, el) {
                var t = $(el);
                var template = t.data('template') || defaultTemplate;
                t.html(template.filledWith(dateInfo));
            });

            return;
        }
        console.log(defaultTemplate.filledWith(dateInfo));
    }

    var _knownNawRuz = {};
    var sunCalculator = CreateSunCalcForBadiDate(getStorage);

    var HolyDays = function () {
        var getBDate = function (d) {
            var afterNawRuz = isAfterNawRuz(d);
            var afterSunset = 0;
            var pmSunset = new Date(d);
            pmSunset.setHours(12);
            pmSunset = sunCalculator.getTimes(pmSunset, _locationLat, _locationLong).sunset;
            if (d.getTime() > pmSunset.getTime()) {
                afterSunset = 1;
            }

            var year = getBadiYear(d);
            var days, month, day;
            if (afterNawRuz) {
                var nawRuz = getNawRuz(d.getFullYear());
                days = dayOfYear(d) - dayOfYear(nawRuz) + afterSunset;
                month = Math.floor(days / 19) + 1;
                day = days % 19;
                if (day == 0) {
                    day = 19;
                    month--;
                }
            }
            else { // before
                var lastDec31 = new Date(d.getFullYear(), 0, 0, 12);
                var lastNawRuz = getNawRuz(lastDec31.getFullYear());
                days = dayOfYear(d) + (dayOfYear(lastDec31) - dayOfYear(lastNawRuz)) + afterSunset;
                month = Math.floor(days / 19) + 1;
                day = days % 19;
                if (day == 0) {
                    day = 19;
                    month--;
                }
                if (month >= 19) {
                    var lastAyyamiHa = new Date(getGDateYMD(year, 19, 1).getTime());
                    lastAyyamiHa.setDate(lastAyyamiHa.getDate() - 1);
                    if (dayOfYear(d) + afterSunset > dayOfYear(lastAyyamiHa)) {
                        month = 19;
                        day = dayOfYear(d) - dayOfYear(lastAyyamiHa) + afterSunset;
                    }
                    else {
                        month = 0;
                    }
                }
            }

            return { y: year, m: month, d: day, eve: afterSunset == 1 };
        };

        var getGDateYMD = function (bYear, bMonth, bDay, fix) {
            // convert bDate to gDate
            if (bMonth < 0 || typeof bMonth == 'undefined') {
                if (fix) {
                    bMonth = 1;
                } else {
                    throw 'invalid Badi date';
                }
            }
            if (bMonth > 19) {
                if (fix) {
                    bMonth = 19;
                } else {
                    throw 'invalid Badi date';
                }
            }
            if (bDay < 1 || !bDay) {
                if (fix) {
                    bDay = 1;
                }
                else {
                    throw 'invalid Badi date';
                }
            }
            if (bDay > 19) {
                if (fix) {
                    bDay = 19;
                }
                else {
                    throw 'invalid Badi date';
                }
            }
            var gYear = bYear + 1843;
            var nawRuz = new Date(gYear, 2, 21 + (_nawRuzOffsetFrom21[bYear] || 0));
            var answer = new Date(nawRuz.getTime());
            answer.setDate(answer.getDate() + (bMonth - 1) * 19 + (bDay - 1));

            if (bMonth == 0 || bMonth == 19) {
                var nextNawRuz = new Date(gYear + 1, 2, 21 + (_nawRuzOffsetFrom21[bYear + 1] || 0));
                var startOfAla = new Date(nextNawRuz.getTime());
                startOfAla.setDate(startOfAla.getDate() - 19);
                if (bMonth == 19) {
                    answer = startOfAla;
                    answer.setDate(answer.getDate() + (bDay - 1));
                }
                else {
                    var firstAyyamiHa = new Date(getGDateYMD(bYear, 18, 19).getTime());
                    firstAyyamiHa.setDate(firstAyyamiHa.getDate() + 1);
                    var lastAyyamiHa = new Date(getGDateYMD(bYear, 19, 1).getTime());
                    lastAyyamiHa.setDate(lastAyyamiHa.getDate() - 1);
                    //console.log('first ' + firstAyyamiHa);
                    //console.log('last #1')
                    //console.log('last ' + lastAyyamiHa);
                    var numDaysInAyyamiHa = daysBetween(firstAyyamiHa, lastAyyamiHa);
                    //console.log(numDaysInAyyamiHa);
                    //debugger;
                    if (bDay > numDaysInAyyamiHa) {
                        if (fix) {
                            bDay = numDaysInAyyamiHa;
                        } else {
                            throw 'invalid Badi date';
                        }
                    }
                    answer = firstAyyamiHa;
                    answer.setDate(answer.getDate() + (bDay - 1));
                }
            }
            return answer;
        };

        var _msInDay = 24 * 60 * 60 * 1000; // hours*minutes*seconds*milliseconds

        var daysBetween = function (d1, d2) {
            return 1 + Math.round(Math.abs((d1.getTime() - d2.getTime()) / _msInDay));
        }; // Badi months - 0=Ayyam-i-Há

        var dayOfYear = function (d) {
            var j1 = new Date(d.getTime());
            j1.setMonth(0, 0);
            return Math.round((d - j1) / 8.64e7);
        };
        var getBadiYear = function (d) {
            return d.getFullYear() - 1843 - (isAfterNawRuz(d) ? 0 : 1);
        }

        var isAfterNawRuz = function (d) {
            return d.getTime() > getNawRuz(d.getFullYear()).getTime();
        };
        var getNawRuz = function (gYear) {
            if (_knownNawRuz[gYear]) {
                return _knownNawRuz[gYear];
            }
            // get NawRuz for this gregorian year
            var bYear = gYear - 1843;
            var nawRuz = new Date(gYear,
              2,
              20 + (_nawRuzOffsetFrom21[bYear] || 0),
              12, 0, 0, 0);

            var eveSunset = new Date(nawRuz.getTime());
            nawRuz = sunCalculator.getTimes(eveSunset, _locationLat, _locationLong).sunset;
            _knownNawRuz[gYear] = nawRuz;
            return nawRuz;
        };

        // table of Naw Ruz dates
        var _nawRuzOffsetFrom21 = {
            // by default and historically, on March 21. If not, year is listed here with the offset... 173 is March 20
            // can be 0, -1, -2? and will never change by more than 1 day between years
            // extracted from UHJ documents and http://www.bahaidate.today/table-of-dates
            173: -1, 174: -1, 175: 0, 176: 0, 177: -1, 178: -1, 179: 0, 180: 0, 181: -1, 182: -1, 183: 0, 184: 0, 185: -1, 186: -1, 187: -1, 188: 0, 189: -1, 190: -1, 191: -1, 192: 0, 193: -1, 194: -1, 195: -1, 196: 0, 197: -1, 198: -1, 199: -1, 200: 0, 201: -1, 202: -1, 203: -1, 204: 0, 205: -1, 206: -1, 207: -1, 208: 0, 209: -1, 210: -1, 211: -1, 212: 0, 213: -1, 214: -1, 215: -1, 216: -1, 217: -1, 218: -1, 219: -1, 220: -1, 221: -1, 222: -1, 223: -1, 224: -1, 225: -1, 226: -1, 227: -1, 228: -1, 229: -1, 230: -1, 231: -1, 232: -1, 233: -1, 234: -1, 235: -1, 236: -1, 237: -1, 238: -1, 239: -1, 240: -1, 241: -1, 242: -1, 243: -1, 244: -1, 245: -1, 246: -1, 247: -1, 248: -1, 249: -2, 250: -1, 251: -1, 252: -1, 253: -2, 254: -1, 255: -1, 256: -1, 257: -1, 258: 0, 259: 0, 260: 0, 261: -1, 262: 0, 263: 0, 264: 0, 265: -1, 266: 0, 267: 0, 268: 0, 269: -1, 270: 0, 271: 0, 272: 0, 273: -1, 274: 0, 275: 0, 276: 0, 277: -1, 278: 0, 279: 0, 280: 0, 281: -1, 282: -1, 283: 0, 284: 0, 285: -1, 286: -1, 287: 0, 288: 0, 289: -1, 290: -1, 291: 0, 292: 0, 293: -1, 294: -1, 295: 0, 296: 0, 297: -1, 298: -1, 299: 0, 300: 0, 301: -1, 302: -1, 303: 0, 304: 0, 305: -1, 306: -1, 307: 0, 308: 0, 309: -1, 310: -1, 311: 0, 312: 0, 313: -1, 314: -1, 315: -1, 316: 0, 317: -1, 318: -1, 319: -1, 320: 0, 321: -1, 322: -1, 323: -1, 324: 0, 325: -1, 326: -1, 327: -1, 328: 0, 329: -1, 330: -1, 331: -1, 332: 0, 333: -1, 334: -1, 335: -1, 336: 0, 337: -1, 338: -1, 339: -1, 340: 0, 341: -1, 342: -1, 343: -1, 344: 0, 345: -1, 346: -1, 347: -1, 348: -1, 349: -1, 350: -1, 351: -1, 352: -1, 353: -1, 354: -1, 355: -1, 356: -1, 357: 0, 358: 0, 359: 0, 360: 0, 361: 0, 362: 0, 363: 0, 364: 0, 365: 0, 366: 0, 367: 0, 368: 0, 369: 0, 370: 0, 371: 0, 372: 0, 373: 0, 374: 0, 375: 0, 376: 0, 377: 0, 378: 0, 379: 0, 380: 0, 381: -1, 382: 0, 383: 0, 384: 0, 385: -1, 386: 0, 387: 0, 388: 0, 389: -1, 390: 0, 391: 0, 392: 0, 393: -1, 394: 0, 395: 0, 396: 0, 397: -1, 398: 0, 399: 0, 400: 0, 401: -1, 402: 0, 403: 0, 404: 0, 405: -1, 406: 0, 407: 0, 408: 0, 409: -1, 410: 0, 411: 0, 412: 0, 413: -1, 414: -1, 415: 0, 416: 0, 417: -1, 418: -1, 419: 0, 420: 0, 421: -1, 422: -1, 423: 0, 424: 0, 425: -1, 426: -1, 427: 0, 428: 0, 429: -1, 430: -1, 431: 0, 432: 0, 433: -1, 434: -1, 435: 0, 436: 0, 437: -1, 438: -1, 439: 0, 440: 0, 441: -1, 442: -1, 443: 0, 444: 0, 445: -1, 446: -1, 447: -1, 448: 0, 449: -1, 450: -1, 451: -1, 452: 0, 453: -1, 454: -1, 455: -1, 456: 0, 457: 0, 458: 0, 459: 0, 460: 1, 461: 0, 462: 0, 463: 0, 464: 1, 465: 0, 466: 0, 467: 0, 468: 1, 469: 0, 470: 0, 471: 0, 472: 1, 473: 0, 474: 0, 475: 0, 476: 1, 477: 0, 478: 0, 479: 0, 480: 0, 481: 0, 482: 0, 483: 0, 484: 0, 485: 0, 486: 0, 487: 0, 488: 0, 489: 0, 490: 0, 491: 0, 492: 0, 493: 0, 494: 0, 495: 0, 496: 0, 497: 0, 498: 0, 499: 0, 500: 0, 501: 0, 502: 0, 503: 0, 504: 0, 505: 0, 506: 0, 507: 0, 508: 0, 509: 0, 510: 0, 511: 0, 512: 0, 513: -1, 514: 0, 515: 0, 516: 0, 517: -1, 518: 0, 519: 0, 520: 0, 521: -1, 522: 0, 523: 0, 524: 0, 525: -1, 526: 0, 527: 0, 528: 0, 529: -1, 530: 0, 531: 0, 532: 0, 533: -1, 534: 0, 535: 0, 536: 0, 537: -1, 538: 0, 539: 0, 540: 0, 541: -1, 542: -1, 543: 0, 544: 0, 545: -1, 546: -1, 547: 0, 548: 0, 549: -1, 550: -1, 551: 0, 552: 0, 553: -1, 554: -1, 555: 0, 556: 0, 557: -1, 558: -1, 559: 0, 560: 0, 561: -1, 562: -1, 563: 0, 564: 0, 565: -1, 566: -1, 567: 0, 568: 0, 569: -1, 570: -1, 571: 0, 572: 0, 573: -1, 574: -1, 575: 0, 576: 0, 577: -1, 578: -1, 579: -1, 580: 0, 581: -1, 582: -1, 583: -1, 584: 0, 585: -1, 586: -1, 587: -1, 588: 0, 589: -1, 590: -1, 591: -1, 592: 0, 593: -1, 594: -1, 595: -1, 596: 0, 597: -1, 598: -1, 599: -1, 600: 0, 601: -1, 602: -1, 603: -1, 604: 0, 605: -1, 606: -1, 607: -1, 608: 0, 609: -1, 610: -1, 611: -1, 612: -1, 613: -1, 614: -1, 615: -1, 616: -1, 617: -1, 618: -1, 619: -1, 620: -1, 621: -1, 622: -1, 623: -1, 624: -1, 625: -1, 626: -1, 627: -1, 628: -1, 629: -1, 630: -1, 631: -1, 632: -1, 633: -1, 634: -1, 635: -1, 636: -1, 637: -1, 638: -1, 639: -1, 640: -1, 641: -1, 642: -1, 643: -1, 644: -1, 645: -2, 646: -1, 647: -1, 648: -1, 649: -2, 650: -1, 651: -1, 652: -1, 653: -2, 654: -1, 655: -1, 656: -1, 657: -1, 658: 0, 659: 0, 660: 0, 661: -1, 662: 0, 663: 0, 664: 0, 665: -1, 666: 0, 667: 0, 668: 0, 669: -1, 670: 0, 671: 0, 672: 0, 673: -1, 674: -1, 675: 0, 676: 0, 677: -1, 678: -1, 679: 0, 680: 0, 681: -1, 682: -1, 683: 0, 684: 0, 685: -1, 686: -1, 687: 0, 688: 0, 689: -1, 690: -1, 691: 0, 692: 0, 693: -1, 694: -1, 695: 0, 696: 0, 697: -1, 698: -1, 699: 0, 700: 0, 701: -1, 702: -1, 703: 0, 704: 0, 705: -1, 706: -1, 707: -1, 708: 0, 709: -1, 710: -1, 711: -1, 712: 0, 713: -1, 714: -1, 715: -1, 716: 0, 717: -1, 718: -1, 719: -1, 720: 0, 721: -1, 722: -1, 723: -1, 724: 0, 725: -1, 726: -1, 727: -1, 728: 0, 729: -1, 730: -1, 731: -1, 732: 0, 733: -1, 734: -1, 735: -1, 736: 0, 737: -1, 738: -1, 739: -1, 740: -1, 741: -1, 742: -1, 743: -1, 744: -1, 745: -1, 746: -1, 747: -1, 748: -1, 749: -1, 750: -1, 751: -1, 752: -1, 753: -1, 754: -1, 755: -1, 756: -1, 757: 0, 758: 0, 759: 0, 760: 0, 761: 0, 762: 0, 763: 0, 764: 0, 765: 0, 766: 0, 767: 0, 768: 0, 769: 0, 770: 0, 771: 0, 772: 0, 773: -1, 774: 0, 775: 0, 776: 0, 777: -1, 778: 0, 779: 0, 780: 0, 781: -1, 782: 0, 783: 0, 784: 0, 785: -1, 786: 0, 787: 0, 788: 0, 789: -1, 790: 0, 791: 0, 792: 0, 793: -1, 794: 0, 795: 0, 796: 0, 797: -1, 798: 0, 799: 0, 800: 0, 801: -1, 802: 0, 803: 0, 804: 0, 805: -1, 806: -1, 807: 0, 808: 0, 809: -1, 810: -1, 811: 0, 812: 0, 813: -1, 814: -1, 815: 0, 816: 0, 817: -1, 818: -1, 819: 0, 820: 0, 821: -1, 822: -1, 823: 0, 824: 0, 825: -1, 826: -1, 827: 0, 828: 0, 829: -1, 830: -1, 831: 0, 832: 0, 833: -1, 834: -1, 835: 0, 836: 0, 837: -1, 838: -1, 839: -1, 840: 0, 841: -1, 842: -1, 843: -1, 844: 0, 845: -1, 846: -1, 847: -1, 848: 0, 849: -1, 850: -1, 851: -1, 852: 0, 853: -1, 854: -1, 855: -1, 856: 0, 857: 0, 858: 0, 859: 0, 860: 1, 861: 0, 862: 0, 863: 0, 864: 1, 865: 0, 866: 0, 867: 0, 868: 1, 869: 0, 870: 0, 871: 0, 872: 0, 873: 0, 874: 0, 875: 0, 876: 0, 877: 0, 878: 0, 879: 0, 880: 0, 881: 0, 882: 0, 883: 0, 884: 0, 885: 0, 886: 0, 887: 0, 888: 0, 889: 0, 890: 0, 891: 0, 892: 0, 893: 0, 894: 0, 895: 0, 896: 0, 897: 0, 898: 0, 899: 0, 900: 0, 901: 0, 902: 0, 903: 0, 904: 0, 905: -1, 906: 0, 907: 0, 908: 0, 909: -1, 910: 0, 911: 0, 912: 0, 913: -1, 914: 0, 915: 0, 916: 0, 917: -1, 918: 0, 919: 0, 920: 0, 921: -1, 922: 0, 923: 0, 924: 0, 925: -1, 926: 0, 927: 0, 928: 0, 929: -1, 930: 0, 931: 0, 932: 0, 933: -1, 934: 0, 935: 0, 936: 0, 937: -1, 938: -1, 939: 0, 940: 0, 941: -1, 942: -1, 943: 0, 944: 0, 945: -1, 946: -1, 947: 0, 948: 0, 949: -1, 950: -1, 951: 0, 952: 0, 953: -1, 954: -1, 955: 0, 956: 0, 957: -1, 958: -1, 959: 0, 960: 0, 961: -1, 962: -1, 963: 0, 964: 0, 965: -1, 966: -1, 967: 0, 968: 0, 969: -1, 970: -1, 971: -1, 972: 0, 973: -1, 974: -1, 975: -1, 976: 0, 977: -1, 978: -1, 979: -1, 980: 0, 981: -1, 982: -1, 983: -1, 984: 0, 985: -1, 986: -1, 987: -1, 988: 0, 989: -1, 990: -1, 991: -1, 992: 0, 993: -1, 994: -1, 995: -1, 996: 0, 997: -1, 998: -1, 999: -1, 1000: 0
        };

        return {
            getBDate: getBDate
        }
    }

    var bYearInVahidNameAr = ",Alif,Bá’,Ab,Dál,Báb,Váv,Abad,Jád,Bahá',Ḥubb,Bahháj,Javáb,Aḥad,Vahháb,Vidád,Badí‘,Bahí,Abhá,Váḥid".split(',');
    var bMonthNameAr = "Ayyám-i-Há,Bahá,Jalál,Jamál,`Azamat,Núr,Rahmat,Kalimát,Kamál,Asmá’,`Izzat,Mashíyyat,`Ilm,Qudrat,Qawl,Masá'il,Sharaf,Sultán,Mulk,`Alá’".split(',');
    var bWeekdayNameAr = ",Jalál,Jamál,Kamál,Fiḍál,‘Idál,Istijlál,Istiqlál".split(','); // from Saturday

    var bYearInVahidMeaning = getMessage("bYearInVahidMeaning").split(',');
    var bMonthMeaning = getMessage("bMonthMeaning").split(',');
    var bWeekdayMeaning = getMessage("bWeekdayMeaning").split(',');

    var gWeekdayLong = getMessage("gWeekdayLong").split(',');
    var gWeekdayShort = getMessage("gWeekdayShort").split(',');
    var gMonthLong = getMessage("gMonthLong").split(',');
    var gMonthShort = getMessage("gMonthShort").split(',');

    var ordinal = getMessage('ordinal').split(',');
    var ordinalNames = getMessage('ordinalNames').split(',');
    var elements = getMessage('elements').split(',');

    var calculateDayInfo = function () {
        //console.log('Calculating date info ({0} - {1} {2},{3})'.filledWith(_locationName, ';no location;location guessed;location known;reused location'.split(';')[localSettings.locationMethod], _locationLat, _locationLong));

        var currentTime = localSettings.currentTime;
        var bNow = new HolyDays().getBDate(currentTime);

        // split the Baha'i day to be "Eve" - sunset to midnight; 
        // and "Morn" - from midnight through to sunset
        var frag1Noon = new Date(currentTime.getTime());
        frag1Noon.setHours(12, 0, 0, 0);
        if (!bNow.eve) {
            // if not already frag1, make it so
            frag1Noon.setDate(frag1Noon.getDate() - 1);
        }
        var frag2Noon = new Date(frag1Noon.getTime());
        frag2Noon.setDate(frag2Noon.getDate() + 1);

        var frag1SunTimes = sunCalculator.getTimes(frag1Noon, _locationLat, _locationLong);
        var frag2SunTimes = sunCalculator.getTimes(frag2Noon, _locationLat, _locationLong);

        di = { // date info
            frag1: frag1Noon,
            frag1Year: frag1Noon.getFullYear(),
            frag1Month: frag1Noon.getMonth(),
            frag1Day: frag1Noon.getDate(),
            frag1Weekday: frag1Noon.getDay(),

            frag2: frag2Noon,
            frag2Year: frag2Noon.getFullYear(),
            frag2Month: frag2Noon.getMonth(), // 0 based
            frag2Day: frag2Noon.getDate(),
            frag2Weekday: frag2Noon.getDay(),

            currentYear: currentTime.getFullYear(),
            currentMonth: currentTime.getMonth(), // 0 based
            currentMonth1: 1 + currentTime.getMonth(),
            currentDay: currentTime.getDate(),
            currentDay00: digitPad2(currentTime.getDate()),
            currentWeekday: currentTime.getDay(),
            currentTime: currentTime,

            startingSunsetDesc12: showTime(frag1SunTimes.sunset),
            startingSunsetDesc24: showTime(frag1SunTimes.sunset, 24),
            endingSunsetDesc12: showTime(frag2SunTimes.sunset),
            endingSunsetDesc24: showTime(frag2SunTimes.sunset, 24),
            frag1SunTimes: frag1SunTimes,
            frag2SunTimes: frag2SunTimes,

            bNow: bNow,
            bDay: bNow.d,
            bWeekday: 1 + (frag2Noon.getDay() + 1) % 7,
            bMonth: bNow.m,
            bYear: bNow.y,
            bVahid: Math.floor(1 + (bNow.y - 1) / 19),

            bDayNameAr: bMonthNameAr[bNow.d],
            bDayMeaning: bMonthMeaning[bNow.d],
            bMonthNameAr: bMonthNameAr[bNow.m],
            bMonthMeaning: bMonthMeaning[bNow.m],

            bEraLong: getMessage('eraLong'),
            bEraAbbrev: getMessage('eraAbbrev'),
            bEraShort: getMessage('eraShort'),

            location: _locationName,
            latitude: _locationLat,
            longitude: _locationLong
        };

        di.bKullishay = Math.floor(1 + (di.bVahid - 1) / 19);
        di.bVahid = di.bVahid - (di.bKullishay - 1) * 19;
        di.bYearInVahid = di.bYear - (di.bVahid - 1) * 19 - (di.bKullishay - 1) * 19 * 19;
        di.bYearInVahidNameAr = bYearInVahidNameAr[di.bYearInVahid];
        di.bYearInVahidMeaning = bYearInVahidMeaning[di.bYearInVahid];
        di.bWeekdayNameAr = bWeekdayNameAr[di.bWeekday];
        di.bWeekdayMeaning = bWeekdayMeaning[di.bWeekday];

        di.bDayOrdinal = di.bDay + getOrdinal(di.bDay);
        di.bVahidOrdinal = di.bVahid + getOrdinal(di.bVahid);
        di.bKullishayOrdinal = di.bKullishay + getOrdinal(di.bKullishay);
        di.bDayOrdinalName = getOrdinalName(di.bDay);
        di.bVahidOrdinalName = getOrdinalName(di.bVahid);
        di.bKullishayOrdinalName = getOrdinalName(di.bKullishay);

        di.bDay00 = digitPad2(di.bDay);
        di.frag1Day00 = digitPad2(di.frag1Day);
        di.currentMonth01 = digitPad2(di.currentMonth1);
        di.frag2Day00 = digitPad2(di.frag2Day);
        di.bMonth00 = digitPad2(di.bMonth);
        di.bYearInVahid00 = digitPad2(di.bYearInVahid);
        di.bVahid00 = digitPad2(di.bVahid);

        di.startingSunsetDesc = localSettings.use24HourClock ? di.startingSunsetDesc24 : di.startingSunsetDesc12;
        di.endingSunsetDesc = localSettings.use24HourClock ? di.endingSunsetDesc24 : di.endingSunsetDesc12;

        di.frag1MonthLong = gMonthLong[di.frag1Month];
        di.frag1MonthShort = gMonthShort[di.frag1Month];
        di.frag1WeekdayLong = gWeekdayLong[di.frag1Weekday];
        di.frag1WeekdayShort = gWeekdayShort[di.frag1Weekday];

        di.frag2MonthLong = gMonthLong[di.frag2Month];
        di.frag2MonthShort = gMonthShort[di.frag2Month];
        di.frag2WeekdayLong = gWeekdayLong[di.frag2Weekday];
        di.frag2WeekdayShort = gWeekdayShort[di.frag2Weekday];

        di.currentMonthLong = gMonthLong[di.currentMonth];
        di.currentMonthShort = gMonthShort[di.currentMonth];
        di.currentWeekdayLong = gWeekdayLong[di.currentWeekday];
        di.currentWeekdayShort = gWeekdayShort[di.currentWeekday];
        //di.currentDateString = moment(di.currentTime).format('YYYY-MM-DD');

        di.currentRelationToSunset = getMessage(bNow.eve ? 'afterSunset' : 'beforeSunset');
        var thisMoment = new Date().getTime();
        di.dayStarted = getMessage(thisMoment > di.frag1SunTimes.sunset.getTime() ? 'dayStartedPast' : 'dayStartedFuture');
        di.dayEnded = getMessage(thisMoment > di.frag2SunTimes.sunset.getTime() ? 'dayEndedPast' : 'dayEndedFuture');
        di.dayStartedLower = di.dayStarted.toLocaleLowerCase();
        di.dayEndedLower = di.dayEnded.toLocaleLowerCase();

        di.bMonthDayYear = getMessage('gMonthDayYear', di);

        if (di.frag1Year != di.frag2Year) {
            // Dec 31/Jan 1
            // Dec 31, 2015/Jan 1, 2015
            di.gCombined = getMessage('gCombined_3', di);
            di.gCombinedY = getMessage('gCombinedY_3', di);
        } else
            if (di.frag1Month != di.frag2Month) {
                // Mar 31/Apr 1
                // Mar 31/Apr 1, 2015
                di.gCombined = getMessage('gCombined_2', di);
                di.gCombinedY = getMessage('gCombinedY_2', di);
            } else {
                // Jul 12/13
                // Jul 12/13, 2015
                di.gCombined = getMessage('gCombined_1', di);
                di.gCombinedY = getMessage('gCombinedY_1', di);
            }

        di.nearestSunset = getMessage(bNow.eve ? "nearestSunsetEve" : "nearestSunsetDay", di);
    }

    function WaitingForLocationInformation() {
        // pull from previous
        if (localSettings.locationMethod != 4) {
            localSettings.originalLocationMethod = localSettings.locationMethod;
        }

        var savedLat = +getStorage('lat');
        if (savedLat) {
            saveLocation(savedLat, +getStorage('long'), getStorage('geoName'), false);
            localSettings.locationMethod = 4;
            //console.log('Reused previous location.');
            continueAfterLocationKnown();
            return;
        }

        // returns true if getting the location. When location is known, will call continueAfterLocationKnown

        if (localSettings.locationMethod == BadiDateLocationChoice.askForUserLocation) {
            if ("geolocation" in navigator) {
                navigator.geolocation.getCurrentPosition(function (position) {
                    saveLocation(position.coords.latitude, position.coords.longitude, '')
                    continueAfterLocationKnown();
                }, function () {
                    // failed
                    console.log('Failed to get location. Will default to unknown.');
                    localSettings.locationMethod = BadiDateLocationChoice.ignoreLocation;
                    WaitingForLocationInformation();
                }, {
                    timeout: 3000,
                    maximumAge: 3 * 60 * 60 * 1000 // 3 hours
                });
                return true;
            }

            // not available in the browser, or user denied request. Fallback to guessing location
        }

        //if (localSettings.locationMethod == BadiDateLocationChoice.guessUserLocation) {
        //    var url = "http://ipinfo.io/geo?json";
        //    var xhr = new XMLHttpRequest();
        //    xhr.onreadystatechange = function () {
        //        if (xhr.readyState == 4 && xhr.status == 200) {
        //            var info = JSON.parse(xhr.responseText);
        //            var loc = info.loc.split(',');
        //            saveLocation(loc[0], loc[1], info.city);
        //            continueAfterLocationKnown();
        //        }
        //    }
        //    xhr.open("GET", url, true);
        //    xhr.timeout = 1000;
        //    xhr.ontimeout = function () { continueAfterLocationKnown(); };
        //    xhr.send();
        //    return true;
        //}

        // use 6:30 as sunset time
        saveLocation(0, 0, '', true);

        continueAfterLocationKnown();
        return false;
    }

    function saveLocation(lat, long, name, saveToStorage) {
        _locationLat = lat;
        _locationLong = long;
        _locationName = name;
        _locationKnown = true;
        setStorage('locationKnown', true);

        if (saveLocation) {
            setStorage('lat', lat);
            setStorage('long', long);
            setStorage('geoName', name);
        }
    }

    function digitPad2(num) {
        return ('00' + num).slice(-2);
    }

    // based on code courtesy of Sunwapta Solutions Inc.
    var ObjectConstant = '$****$';

    function setStorage(key, value) {
        /// <summary>Save this value in the browser's local storage. Dates do NOT get returned as full dates!</summary>
        /// <param name="key" type="string">The key to use</param>
        /// <param name="value" type="string">The value to store. Can be a simple or complex object.</param>
        if (typeof value === 'object' || typeof value === 'boolean') {
            var strObj = JSON.stringify(value);
            value = ObjectConstant + strObj;
        }

        localStorage[key] = value + "";
    }

    function showTime(d, hoursType) {
        //var time = ('0' + this.getHours()).slice(-2) + ':' + ('0' + this.getMinutes()).slice(-2);
        var show24hour = hoursType == 24;
        var pm = d.getHours() >= 12;
        var hours = d.getHours() > 12 && !show24hour ? d.getHours() - 12 : d.getHours();
        var minutes = d.getMinutes();
        var time = hours + ':' + ('0' + minutes).slice(-2) + (!show24hour ? (pm ? ' pm' : ' am') : '');
        if (hours == 12 && minutes == 0 && !show24hour) {
            time = '12:00 noon';
        }
        //  if(includeHtml){
        //  return '<span title="' + this + '">' + time + '</span>';
        //  }
        return time;
        //return time;
    };

    function getStorage(key, defaultValue) {
        /// <summary>Get a value from storage.</summary>
        var checkForObject = function (obj) {
            if (obj.substring(0, ObjectConstant.length) == ObjectConstant) {
                obj = JSON.parse(obj.substring(ObjectConstant.length));
            }
            return obj;
        };

        var value = localStorage[key];
        if (typeof value !== 'undefined' && value != null) {
            return checkForObject(value);
        }
        return defaultValue;
    }

    function getOrdinal(num) {
        return ordinal[num] || ordinal[0];
    }
    function getOrdinalName(num) {
        return ordinalNames[num];
    }

    function getMessage(key, obj, defaultValue) {
        var lang = localSettings.language;
        var rawMsg;
        if (lang != 'en') {
            var langMessages = _messages[lang];
            if (langMessages) {
                rawMsg = langMessages[key];
            }
        }
        if (!rawMsg) {
            rawMsg = _messages['en'][key];
        }

        var msg = (rawMsg && rawMsg.message) || defaultValue || ('{' + key + '}');
        return typeof obj === 'undefined' ? msg : msg.filledWith(obj);
    }

    function continueAfterLocationKnown() {
        calculateDayInfo();
        setTimeout(function () {
            localSettings.onReady(di);
        }, 0);
    }

    WaitingForLocationInformation()

    var publicInterface = {
        /**
         * Get the DayInfo object with full details about this day.
         * Only useful if locationMethod is BadiDateLocationChoice.ignoreLocation, or the location has already been learned.
         */
        getDateInfo: function () { return di; },
        /**
         * Refreshes the dayInfo.
         * @param {newSettings} optional. Can change any of the original settings.
         * @param {refreshLocation} optional. If true, re-determine the location.
         */
        refresh: function (newSettings, refreshLocation) {
            if (refreshLocation) {
                setStorage('lat', 0);
            }
            applySettings(newSettings);
            WaitingForLocationInformation();
        }
    }

    return publicInterface;
}










// Old object for backward compatibility with features observed to be used 'in the wild' from the original BahaiDate version.
// Original code by Glen Little. Developed in April 1999 / Splendor 156
var BahaiDate = function () {
    var di = BadiDateToday({
        // to be backward compatible with the original, must not try to get user's location!
        locationMethod: BadiDateLocationChoice.ignoreLocation
    }).getDateInfo();

    return {
        day: di.bDay,
        month: di.bMonth,
        year: di.bYear,
        dayName: function () {
            return di.bDayNameAr;
        },
        monthName: function () {
            return di.bMonthNameAr;
        }
    };
};





var CreateSunCalcForBadiDate = function (getStorage) {
    'use strict';
    /*
     Adapted from SunCalc. 
     Glen: Moon and other unused calcs removed. 
  
     SunCalc is a JavaScript library for calculating sun/mooon position and light phases.
     (c) 2011-2014, Vladimir Agafonkin
     https://github.com/mourner/suncalc
    */
    function toJulian(n) { return n.valueOf() / dayMs - .5 + J1970 } function fromJulian(n) { return new Date((n + .5 - J1970) * dayMs) } function toDays(n) { return toJulian(n) - J2000 } function rightAscension(n, a) { return atan(sin(n) * cos(e) - tan(a) * sin(e), cos(n)) } function declination(n, a) { return asin(sin(a) * cos(e) + cos(a) * sin(e) * sin(n)) } function solarMeanAnomaly(n) { return rad * (357.5291 + .98560028 * n) } function eclipticLongitude(n) { var a = rad * (1.9148 * sin(n) + .02 * sin(2 * n) + 3e-4 * sin(3 * n)), t = 102.9372 * rad; return n + a + t + PI } function julianCycle(n, a) { return Math.round(n - J0 - a / (2 * PI)) } function approxTransit(n, a, t) { return J0 + (n + a) / (2 * PI) + t } function solarTransitJ(n, a, t) { return J2000 + n + .0053 * sin(a) - .0069 * sin(2 * t) } function hourAngle(n, a, t) { return acos((sin(n) - sin(a) * sin(t)) / (cos(a) * cos(t))) } function getSetJ(n, a, t, r, i, s, o) { var e = hourAngle(n, t, r), u = approxTransit(e, a, i); return solarTransitJ(u, s, o) } var PI = Math.PI, sin = Math.sin, cos = Math.cos, tan = Math.tan, asin = Math.asin, atan = Math.atan2, acos = Math.acos, rad = PI / 180, dayMs = 864e5, J1970 = 2440588, J2000 = 2451545, e = 23.4397 * rad, SunCalc = {}, times = SunCalc.times = [[-.833, "sunrise", "sunset"]], J0 = 9e-4; SunCalc.getTimes = function (n, a, t) { if (!getStorage("locationKnown", !1)) { var r = new Date(n.getTime()); return r.setHours(18, 30, 0, 0), { sunset: r } } var i, s, o, e, u, c = rad * -t, l = rad * a, f = toDays(n), J = julianCycle(f, c), d = approxTransit(0, c, J), m = solarMeanAnomaly(d), M = eclipticLongitude(m), g = declination(M, 0), h = solarTransitJ(d, m, M), y = { solarNoon: fromJulian(h), nadir: fromJulian(h - .5) }; for (i = 0, s = times.length; s > i; i += 1) o = times[i], e = getSetJ(o[0] * rad, c, l, g, J, m, M), u = h - (e - h), y[o[1]] = fromJulian(u), y[o[2]] = fromJulian(e); return y }; return SunCalc;
};

//String.prototype.filledWith = function () {
//    /// <summary>Similar to C# String.Format...  in two modes:
//    /// 1) Replaces {0},{1},{2}... in the string with values from the list of arguments. 
//    /// 2) If the first and only parameter is an object, replaces {xyz}... (only names allowed) in the string with the properties of that object. 
//    /// Notes: the { } symbols cannot be escaped and should only be used for replacement target tokens;  only a single pass is done. 
//    /// </summary>
//    for (var values = "object" == typeof arguments[0] && 1 === arguments.length ? arguments[0] : arguments, testForFunc = /^#/, testForElementAttribute = /^\*/, testDoNotEscapeHtml = /^\^/, testDoNotEscpaeHtmlButToken = /^-/, testDoNotEscpaeHtmlButSinglQuote = /^\>/, extractTokens = /{([^{]+?)}/g, replaceTokens = function (input) { return input.replace(extractTokens, function () { var token = arguments[1], value = void 0; try { if (" " === token[0]) value = "{" + token + "}"; else if (null === values) value = ""; else if (testForFunc.test(token)) try { value = eval(token.substring(1)) } catch (e) { value = "{" + token + "}" } else if (testForElementAttribute.test(token)) value = quoteattr(values[token.substring(1)]); else if (testDoNotEscpaeHtmlButToken.test(token)) value = values[token.substring(1)].replace(/{/g, "&#123;"); else if (testDoNotEscpaeHtmlButSinglQuote.test(token)) value = values[token.substring(1)].replace(/'/g, "%27"); else if (testDoNotEscapeHtml.test(token)) value = values[token.substring(1)]; else { var toEscape = values[token]; value = "undefined" == typeof toEscape || null === toEscape ? "" : ("" + toEscape).replace(/&/g, "&").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/'/g, "&#39;").replace(/"/g, "&quot;").replace(/{/g, "&#123;") } } catch (err) { throw console.log("filledWithError:\n" + err + "\ntoken:" + token + "\nvalue:" + value + "\ntemplate:" + input + "\nall values:\n"), console.log(values), "Error in Filled With" } return "undefined" == typeof value || null == value ? "" : "" + value }) }, result = replaceTokens(this), lastResult = ""; lastResult != result;) lastResult = result, result = replaceTokens(result); return result
//};
