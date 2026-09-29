-- Script de criação da tabela Produto (SQLite).
--
-- A aplicação executa este mesmo arquivo toda vez que inicia. O IF NOT EXISTS
-- torna a execução repetível: na primeira vez cria a tabela, nas seguintes
-- não faz nada e os dados continuam lá.
--
-- Para criar o banco manualmente, sem a aplicação (bash, cmd ou PowerShell):
--   sqlite3 produtos.db ".read database/criar-tabela-produto.sql"

CREATE TABLE IF NOT EXISTS Produto (
    Id        INTEGER       PRIMARY KEY AUTOINCREMENT,
    Nome      TEXT          NOT NULL,
    Preco     DECIMAL(10,2) NOT NULL CHECK (Preco >= 0),
    Estoque   INTEGER       NOT NULL CHECK (Estoque >= 0),
    Categoria TEXT          NOT NULL
);
