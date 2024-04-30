
let gameList = ['Pac-man', 'Donkey Kong', 'Super Mario', 'Mario Kart']
let gamerList = ['LiekGeek', 'LX360', 'Synergy']

let url = 'http://localhost:4972/api/v1.0/Scores/'

let getUrl = 'http://localhost:5618/'


setInterval(() => {
    
    let gamer = gamerList[Math.floor(Math.random() * gamerList.length)];
    let game = gameList[Math.floor(Math.random() * gameList.length)];

    let score = Math.floor(Math.random() * 100);

    fetch(`${url}${gamer}/${game}`, {
        method: 'POST',
        headers: { 
            "Content-Type": "application/json"
        },
        body: score

    }).then((response) => {
        console.log(`Added score {${score}} for gamer {${gamer}} on game {${game}}`);
    })
    
}, 5000);


setInterval(() => {
    let limit = Math.floor((Math.random() * 11) -1 );
    fetch(`${getUrl}?limit=${limit}`)
}, 2000);