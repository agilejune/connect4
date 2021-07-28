const shortid = require('shortid');

class GameController {
    constructor() {
        this.players = new Map();
        this._playerNameToIdMap = new Map();
        this.scores = {};
        this.games = new Map();

        this.initScore();
    }

    initScore() {
        for (let i = 0; i < 20; ++i) {
            this.scores[`Player ${i+1}`] = 100 * i;
        }
    }
    
    getScore(username) {
        return this.scores[username] || 0;
    }
    
    setScore(username, score) {
        this.scores[username] = score;
    }
    
    login(thisPlayerId, username) {
        console.log(`Client try to login: player=(${thisPlayerId}:${username})`);
    
        if (!this._playerNameToIdMap.has(username)) {
            const thisPlayer = {
                id: thisPlayerId,
                name: username,
                score: this.getScore(username),
                gameid: null,
            };
    
            console.log(`Player login success: player=(${thisPlayer.id}:${thisPlayer.name})`);
    
            this.players.set(thisPlayer.id, thisPlayer);
            this._playerNameToIdMap.set(thisPlayer.name, thisPlayer.id);
    
            return thisPlayer;
        } else {
            console.log(`Player login failed. Player already logged in: ${thisPlayerId} with ${username}`);
        }
    }

    createGame(thisPlayer, rows, cols) {
        if (thisPlayer.gameid == null) {
            const game = {
                id: shortid.generate(),
                owner: thisPlayer.id,
                rows: rows,
                cols: cols,
                players: [thisPlayer.id],
            };
            this.games.set(game.id, game);
    
            console.log(`Player create game: player=(${thisPlayer.id}:${thisPlayer.name}), game=${game.id}`);
    
            this.enterGame(thisPlayer, game);
    
            return game;
        } else {
            console.log(`Player already in game: player=(${thisPlayer.id}:${thisPlayer.name}), game=${thisPlayer.gameid}`);
        }
    }

    joinGame(thisPlayer, gameid) {
        const game = this.games.has(gameid) ? this.games.get(gameid) : null;
    
        console.log(`Player try to join game: player=(${thisPlayer.name}:${thisPlayer.id}), game=${gameid}`);
    
        if (!thisPlayer.gameid && game && game.players.length < 2) {
            this.game.players.push(thisPlayer.id);
    
            this.enterGame(thisPlayer, game);
    
            return game;
        } else {
            console.log(`Player join game failed: player=(${thisPlayer.name}:${thisPlayer.id}), game=${gameid}`);
        }
    }
    
    enterGame(thisPlayer, game) {
        thisPlayer.gameid = game.id;
    }
    
    leaveGame(thisPlayer) {
        const gameid = thisPlayer.gameid;
        console.log(`Player try to leave game: player=(${thisPlayer.name}:${thisPlayer.id}), game=${gameid}`);
    
        const game = this.games.get(gameid);
    
        thisPlayer.gameid = null;
        game.players.splice(game.players.indexOf(thisPlayer.id), 1);
    
        if (!game.players.length) {
            console.log(`Destroying empty game: game=${gameid}`);
    
            this.games.delete(gameid);
        }

        return game;
    }
    
    logout(thisPlayer) {
        this.players.delete(thisPlayer.id);
        this._playerNameToIdMap.delete(thisPlayer.name);
        console.log(`Player deleted: player=(${thisPlayer.id}:${thisPlayer.name})`);
    }
}

module.exports = GameController;
