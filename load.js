
let gameList = ['Pac-Man', 'Donky-Kong', 'Super Mario', 'Paperboy']
let gamerList = ['LiekGeek', 'LX360', 'Techorama']

let url = 'https://symmetrical-spoon-r4r9x97xqgp36rr-4972.preview.app.github.dev/api/v1.0/Scores/'


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

    })
}, 100);